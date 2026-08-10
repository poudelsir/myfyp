// Admin Dashboard (Areas/Admin/Views/Dashboard/Index.cshtml). All four charts render
// immediately from data the server already embedded — no fetch(), no loading state.
// Distinct from admin-insights.js, which backs the separate AI-narrated /Admin/Insights
// page and additionally loads a Gemini summary via fetch().
//
// The charts live inside the "Analytics" Bootstrap tab-pane, which is hidden
// (display: none) at DOMContentLoaded — Chart.js reads a 0x0 canvas at construction
// time in that state and never recovers on its own once the pane becomes visible, so
// every chart is resize()'d again on the tab's shown.bs.tab event.
(function () {
    "use strict";

    var charts = [];

    document.addEventListener("DOMContentLoaded", function () {
        charts = [
            renderRegistrationGrowthChart(),
            renderListingStatusChart(),
            renderOrderStatusChart(),
            renderReviewRatingChart(),
        ];

        var analyticsTab = document.getElementById("dashboard-tab-analytics");
        if (analyticsTab) {
            analyticsTab.addEventListener("shown.bs.tab", function () {
                charts.forEach(function (chart) {
                    if (chart) {
                        chart.resize();
                    }
                });
            });
        }
    });

    function renderRegistrationGrowthChart() {
        var canvas = document.getElementById("registrationGrowthChart");
        var empty = document.getElementById("registrationGrowthEmpty");
        var growth = readJson("registrationGrowthData");

        var total = (growth || []).reduce(function (sum, item) { return sum + item.count; }, 0);
        if (!window.Chart || !canvas || !growth || growth.length === 0 || total === 0) {
            toggleEmpty(canvas, empty);
            return null;
        }

        return new window.Chart(canvas, {
            type: "line",
            data: {
                labels: growth.map(function (item) { return item.name; }),
                datasets: [{
                    label: "New users",
                    data: growth.map(function (item) { return item.count; }),
                    borderColor: "#0d6efd",
                    backgroundColor: "rgba(13, 110, 253, 0.15)",
                    fill: true,
                    tension: 0.3,
                }],
            },
            options: {
                responsive: true,
                plugins: { legend: { display: false } },
                scales: { y: { beginAtZero: true, ticks: { precision: 0 } } },
            },
        });
    }

    function renderListingStatusChart() {
        var canvas = document.getElementById("listingStatusChart");
        var empty = document.getElementById("listingStatusEmpty");
        var stats = readJson("listingStatusData");

        var total = stats ? Object.keys(stats).reduce(function (sum, key) { return sum + stats[key]; }, 0) : 0;
        if (!window.Chart || !canvas || total === 0) {
            toggleEmpty(canvas, empty);
            return null;
        }

        var labels = ["Draft", "Pending", "Active", "Reserved", "Sold", "Donated", "Archived", "Rejected", "Out of Stock"];
        var data = [stats.draft, stats.pending, stats.active, stats.reserved, stats.sold, stats.donated, stats.archived, stats.rejected, stats.outOfStock];
        var colors = ["#6c757d", "#0dcaf0", "#198754", "#ffc107", "#0d6efd", "#fd7e14", "#adb5bd", "#dc3545", "#d63384"];

        return new window.Chart(canvas, {
            type: "doughnut",
            data: {
                labels: labels,
                datasets: [{ data: data, backgroundColor: colors }],
            },
            options: {
                responsive: true,
                plugins: { legend: { position: "bottom" } },
            },
        });
    }

    function renderOrderStatusChart() {
        var canvas = document.getElementById("orderStatusChart");
        var empty = document.getElementById("orderStatusEmpty");
        var stats = readJson("orderStatusData");

        if (!window.Chart || !canvas || !stats || stats.totalOrders === 0) {
            toggleEmpty(canvas, empty);
            return null;
        }

        var labels = ["Pending", "Confirmed", "Ready for Pickup", "Completed", "Cancelled"];
        var data = [stats.pendingCount, stats.confirmedCount, stats.readyForPickupCount, stats.completedCount, stats.cancelledCount];
        var colors = ["#0dcaf0", "#0d6efd", "#ffc107", "#198754", "#dc3545"];

        return new window.Chart(canvas, {
            type: "doughnut",
            data: {
                labels: labels,
                datasets: [{ data: data, backgroundColor: colors }],
            },
            options: {
                responsive: true,
                plugins: { legend: { position: "bottom" } },
            },
        });
    }

    function renderReviewRatingChart() {
        var canvas = document.getElementById("reviewRatingChart");
        var empty = document.getElementById("reviewRatingEmpty");
        var distribution = readJson("reviewRatingData");

        var total = (distribution || []).reduce(function (sum, count) { return sum + count; }, 0);
        if (!window.Chart || !canvas || !distribution || total === 0) {
            toggleEmpty(canvas, empty);
            return null;
        }

        return new window.Chart(canvas, {
            type: "bar",
            data: {
                labels: ["1 star", "2 stars", "3 stars", "4 stars", "5 stars"],
                datasets: [{
                    label: "Reviews",
                    data: distribution,
                    backgroundColor: "#ffc107",
                    borderRadius: 4,
                }],
            },
            options: {
                responsive: true,
                plugins: { legend: { display: false } },
                scales: { y: { beginAtZero: true, ticks: { precision: 0 } } },
            },
        });
    }

    function toggleEmpty(canvas, empty) {
        if (canvas) {
            canvas.classList.add("d-none");
        }
        if (empty) {
            empty.classList.remove("d-none");
        }
    }

    function readJson(elementId) {
        var el = document.getElementById(elementId);
        if (!el) {
            return null;
        }

        try {
            return JSON.parse(el.textContent);
        } catch (e) {
            return null;
        }
    }
})();
