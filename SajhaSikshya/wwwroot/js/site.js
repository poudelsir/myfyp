// SajhaSikshya — global bootstrap script.
// Runs on every page (both the dashboard shell and the auth/marketing layout).
// Keep this file limited to behavior that truly applies everywhere; page- or
// feature-specific scripts belong in their own file (see dashboard.js, dark-mode.js).

(function () {
    "use strict";

    document.addEventListener("DOMContentLoaded", function () {
        if (window.lucide) {
            window.lucide.createIcons();
        }
    });

    // Delegated confirm for any form with class "js-delete-form" and a
    // data-confirm message. Reading the message from a data-attribute (rather
    // than inlining it into an onsubmit="" handler) keeps it safe even when the
    // confirmation text contains a name with an apostrophe or quote.
    document.addEventListener("submit", function (event) {
        var form = event.target.closest(".js-delete-form");
        if (form && !window.confirm(form.dataset.confirm || "Are you sure?")) {
            event.preventDefault();
        }
    }, true);
})();
