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
        markScrollableTables();
    });

    // Bootstrap's .table-responsive already lets a wide data table (Admin Users,
    // My Listings, My Orders, etc.) scroll horizontally on narrow screens — but
    // with no visible scrollbar and no other hint, a column like Status or the
    // row actions can end up completely undiscoverable off-screen. This just
    // marks which ones actually need the hint (only when there's really more to
    // see) so the CSS can show a "more columns →" affordance instead of nothing.
    function markScrollableTables() {
        var containers = document.querySelectorAll(".table-responsive");
        if (containers.length === 0) {
            return;
        }

        function update() {
            containers.forEach(function (el) {
                el.classList.toggle("has-more-columns", el.scrollWidth > el.clientWidth + 2);
            });
        }

        containers.forEach(function (el) {
            el.addEventListener("scroll", function () {
                el.classList.toggle("has-more-columns", el.scrollWidth - el.scrollLeft > el.clientWidth + 2);
            });
        });

        update();
        window.addEventListener("resize", update);
    }

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

    // Generic horizontal-scroll arrow: any button with [data-scroll-target]
    // nudges the element with that id one "page" to the right, wrapping back
    // to the start once it reaches the end — used by the homepage's category
    // rail and any future horizontally-scrolling row.
    document.addEventListener("click", function (event) {
        var button = event.target.closest("[data-scroll-target]");
        if (!button) {
            return;
        }

        var target = document.getElementById(button.dataset.scrollTarget);
        if (!target) {
            return;
        }

        var atEnd = target.scrollLeft + target.clientWidth >= target.scrollWidth - 4;
        target.scrollBy({ left: atEnd ? -target.scrollWidth : target.clientWidth * 0.8, behavior: "smooth" });
    });
})();
