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
        if (registration.waiting && navigator.serviceWorker.controller) {
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
                window.location.reload();
                resolve();
            };

            navigator.serviceWorker.addEventListener('controllerchange', reload, { once: true });

            if (window.karaokeListAppUpdates.registration?.waiting) {
                window.karaokeListAppUpdates.registration.waiting.postMessage({ type: 'SKIP_WAITING' });
            } else {
                reload();
            }
        });
    }
};
