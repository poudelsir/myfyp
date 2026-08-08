// Top announcement bar (Views/Shared/_MarketplaceLayout.cshtml) — dismiss state
// persists in localStorage, same convention as dark-mode.js's theme preference.
(function () {
    "use strict";

    var STORAGE_KEY = "sajhasikshya-announcement-dismissed";

    document.addEventListener("DOMContentLoaded", function () {
        var bar = document.getElementById("announcementBar");
        if (!bar) {
            return;
        }

        if (localStorage.getItem(STORAGE_KEY) === "true") {
            bar.classList.add("d-none");
            return;
        }

        var closeBtn = document.getElementById("announcementBarClose");
        closeBtn?.addEventListener("click", function () {
            bar.classList.add("d-none");
            localStorage.setItem(STORAGE_KEY, "true");
        });
    });
})();
