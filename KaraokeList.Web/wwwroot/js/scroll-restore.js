// Scroll restoration for My Songs list → detail → back navigation.
window.karaokeListScrollRestore = {
    isBackNavigation: false,
    initialized: false,

    init: function () {
        if (this.initialized) {
            return;
        }

        this.initialized = true;
        var self = this;
        window.addEventListener('popstate', function () {
            self.isBackNavigation = true;
        });
    },

    consumeBackNavigation: function () {
        var was = this.isBackNavigation;
        this.isBackNavigation = false;
        return was;
    },

    navigateBack: function () {
        history.back();
    },

    getElementTop: function (selector) {
        var el = document.querySelector(selector);
        if (!el) {
            return 0;
        }

        return el.getBoundingClientRect().top + window.scrollY;
    },

    scrollToListIndex: function (listSelector, itemSize, index) {
        var top = this.getElementTop(listSelector);
        var target = top + index * itemSize - window.innerHeight * 0.3;
        window.scrollTo({ top: Math.max(0, target), behavior: 'instant' });
    },

    scrollToSong: function (songId) {
        var el = document.querySelector('[data-song-id="' + songId + '"]');
        if (!el) {
            return false;
        }

        el.scrollIntoView({ block: 'center', behavior: 'instant' });
        return true;
    },

    scrollToSongWithRetry: function (songId, listSelector, itemSize, index, maxAttempts) {
        maxAttempts = maxAttempts || 8;
        var self = this;

        if (listSelector && itemSize >= 0 && index >= 0) {
            self.scrollToListIndex(listSelector, itemSize, index);
        }

        var attempt = 0;
        function tryScroll() {
            attempt++;
            if (self.scrollToSong(songId) || attempt >= maxAttempts) {
                return;
            }

            requestAnimationFrame(tryScroll);
        }

        requestAnimationFrame(function () {
            requestAnimationFrame(tryScroll);
        });
    }
};

window.karaokeListScrollRestore.init();
