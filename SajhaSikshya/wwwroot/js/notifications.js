// SajhaSikshya — real-time notifications placeholder.
// The SignalR hub (/hubs/notifications) and its UI (bell icon, dropdown panel)
// are wired up as scaffolding; no notification-producing features exist yet,
// so this intentionally does not open a connection until there's something to
// push. Flip ENABLE_LIVE_CONNECTION on once a feature starts raising events.

(function () {
    "use strict";

    const ENABLE_LIVE_CONNECTION = false;

    document.addEventListener("DOMContentLoaded", function () {
        const isAuthenticated = document.body.dataset.authenticated === "true";
        if (!ENABLE_LIVE_CONNECTION || !isAuthenticated || typeof signalR === "undefined") {
            return;
        }

        const connection = new signalR.HubConnectionBuilder()
            .withUrl("/hubs/notifications")
            .withAutomaticReconnect()
            .build();

        connection.on("ReceiveNotification", function () {
            const dot = document.getElementById("notificationDot");
            dot?.classList.remove("d-none");
        });

        connection.start().catch(function (err) {
            console.error("Notification hub connection failed:", err);
        });
    });
})();
