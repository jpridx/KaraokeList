// PWA update detection and apply for KaraokeList (published/offline mode).
window.karaokeListAppUpdates = {
    dotNetRef: null,
    registration: null,
    updateCheckIntervalId: null,

    init: function (dotNetRef) {
        this.dotNetRef = dotNetRef;
        if (!('serviceWorker' in navigator)) {
            return;
        }

        this.registerAndWatch().catch(function (err) {
            console.warn('KaraokeList app update watcher failed to start.', err);
        });
    },

    dispose: function () {
        this.dotNetRef = null;
        if (this.updateCheckIntervalId !== null) {
            clearInterval(this.updateCheckIntervalId);
            this.updateCheckIntervalId = null;
        }
    },

    registerAndWatch: async function () {
        this.registration = await navigator.serviceWorker.getRegistration();
        if (!this.registration) {
            this.registration = await navigator.serviceWorker.register('service-worker.js', {
                updateViaCache: 'none'
            });
        }

        this.watchRegistration(this.registration);
        await this.registration.update();

        this.updateCheckIntervalId = setInterval(function () {
            window.karaokeListAppUpdates.registration?.update();
        }, 60 * 60 * 1000);

        document.addEventListener('visibilitychange', function () {
            if (document.visibilityState === 'visible') {
                window.karaokeListAppUpdates.registration?.update();
            }
        });
    },

    watchRegistration: function (registration) {
        // After a user-initiated "Refresh now", applyUpdate() sets this flag before
        // reloading. On the next load the waiting worker may still be present (iOS PWA
        // often does not complete SW activation across a reload), so we skip the
        // immediate notification for that same waiting worker. Genuine new updates are
        // still caught by the updatefound → statechange listener below.
        var justApplied = sessionStorage.getItem('sw-update-applied');
        if (justApplied) {
            sessionStorage.removeItem('sw-update-applied');
        }

        if (!justApplied && registration.waiting && navigator.serviceWorker.controller) {
            this.notifyUpdate();
        }

        registration.addEventListener('updatefound', function () {
            var newWorker = registration.installing;
            if (!newWorker) {
                return;
            }

            newWorker.addEventListener('statechange', function () {
                if (newWorker.state === 'installed' && navigator.serviceWorker.controller) {
                    window.karaokeListAppUpdates.notifyUpdate();
                }
            });
        });
    },

    notifyUpdate: function () {
        if (this.dotNetRef) {
            this.dotNetRef.invokeMethodAsync('OnUpdateAvailable');
        }
    },

    applyUpdate: function () {
        return new Promise(function (resolve) {
            if (!('serviceWorker' in navigator)) {
                window.location.reload();
                resolve();
                return;
            }

            var reloaded = false;
            var reload = function () {
                if (reloaded) {
                    return;
                }

                reloaded = true;
                // Signal the next session to skip the immediate waiting-worker check.
                // iOS PWA often keeps the old SW in 'waiting' across a reload, which
                // would re-trigger the banner immediately. This flag suppresses that
                // one re-notification while still allowing genuinely new updates through.
                try { sessionStorage.setItem('sw-update-applied', '1'); } catch (e) { }
                window.location.reload();
                resolve();
            };

            navigator.serviceWorker.addEventListener('controllerchange', reload, { once: true });

            if (window.karaokeListAppUpdates.registration?.waiting) {
                window.karaokeListAppUpdates.registration.waiting.postMessage({ type: 'SKIP_WAITING' });
                // Fallback: if controllerchange never fires (known iOS timing issue),
                // wait a bit longer before forcing reload so activation has time to finish.
                setTimeout(reload, 8000);
            } else {
                reload();
            }
        });
    },

    clearCacheAndReload: async function () {
        try {
            if ('serviceWorker' in navigator) {
                const registrations = await navigator.serviceWorker.getRegistrations();
                await Promise.all(registrations.map(r => r.unregister()));
            }

            if ('caches' in window) {
                const keys = await caches.keys();
                await Promise.all(keys.map(k => caches.delete(k)));
            }
        } catch (e) {
            console.warn('KaraokeList: cache clear encountered an error, reloading anyway.', e);
        }

        window.location.reload();
    }
};
