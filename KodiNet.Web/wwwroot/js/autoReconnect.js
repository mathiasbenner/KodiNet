function setupAutoReconnect() {
    // Écouter les événements de déconnexion
    window.addEventListener('blazor.disconnected', () => {
        console.log("Déconnecté, tentative de reconnexion...");
        // Forcer une reconnexion après un délai
        setTimeout(() => {
            window.location.reload(); // Rafraîchir la page pour forcer une reconnexion
        }, 2000);
    });

    // Écouter le retour de focus
    window.addEventListener('focus', () => {
        // Vérifier si l'application est déconnectée
        if (window.blazorConnection) {
            // Forcer une reconnexion si nécessaire
            window.blazorConnection.start().catch(() => {
                console.log("Échec de la reconnexion après retour de focus.");
            });
        }
    });
}
