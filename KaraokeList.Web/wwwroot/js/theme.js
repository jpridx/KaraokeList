// Light/dark theme switching for KaraokeList (Bootstrap + Syncfusion Fluent 2).
window.karaokeListTheme = {
    storageKey: 'karaoke.theme.preference',
    syncfusionVersion: '34.1.29',
    mediaQuery: null,
    mediaHandler: null,

    getStoredPreference: function () {
        try {
            var raw = localStorage.getItem(this.storageKey);
            if (raw === null || raw === '') {
                return 'System';
            }

            if (raw === '0' || raw === '1' || raw === '2') {
                return ['System', 'Light', 'Dark'][Number(raw)] || 'System';
            }

            return raw;
        } catch (e) {
            return 'System';
        }
    },

    resolveEffectiveTheme: function (preference) {
        if (preference === 'Dark' || preference === 2 || preference === '2') {
            return 'dark';
        }

        if (preference === 'Light' || preference === 1 || preference === '1') {
            return 'light';
        }

        return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
    },

    applyTheme: function (preference) {
        var effective = this.resolveEffectiveTheme(preference);
        document.documentElement.setAttribute('data-bs-theme', effective);

        var link = document.getElementById('syncfusion-theme');
        if (link) {
            link.href = effective === 'dark'
                ? 'https://cdn.syncfusion.com/blazor/' + this.syncfusionVersion + '/styles/fluent2-dark.css'
                : 'https://cdn.syncfusion.com/blazor/' + this.syncfusionVersion + '/styles/fluent2-lite.css';
        }

        var meta = document.querySelector('meta[name="theme-color"]');
        if (meta) {
            meta.content = effective === 'dark' ? '#1e1e1e' : '#1b6ec2';
        }
    },

    initEarly: function () {
        this.applyTheme(this.getStoredPreference());
    },

    init: function () {
        if (this.mediaQuery) {
            return;
        }

        var self = this;
        this.mediaQuery = window.matchMedia('(prefers-color-scheme: dark)');
        this.mediaHandler = function () {
            var preference = self.getStoredPreference();
            if (preference === 'System' || preference === 0 || preference === '0') {
                self.applyTheme('System');
            }
        };

        this.mediaQuery.addEventListener('change', this.mediaHandler);
    },

    toStorageValue: function (preference) {
        if (preference === 'Dark' || preference === 2 || preference === '2') {
            return '2';
        }

        if (preference === 'Light' || preference === 1 || preference === '1') {
            return '1';
        }

        return '0';
    },

    setPreference: function (preference) {
        try {
            localStorage.setItem(this.storageKey, this.toStorageValue(preference));
        } catch (e) {
            // Ignore storage failures; still apply the theme for this session.
        }

        this.applyTheme(preference);
    }
};
