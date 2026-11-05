(() => {
  if (window.__sageHistoryGuard) return;
  window.__sageHistoryGuard = true;

  // bfcache: al volver con Atrás el navegador puede mostrar una página vieja
  // (dashboard) aunque la sesión ya no exista. Forzar recarga revalida auth.
  window.addEventListener("pageshow", (event) => {
    if (event.persisted) {
      window.location.reload();
    }
  });

  // Tras login: la bandera la pone login.js; al llegar al shell autenticado
  // reemplazamos la entrada actual para no dejar basura de Continue.
  if (sessionStorage.getItem("sage-just-logged-in") === "1") {
    sessionStorage.removeItem("sage-just-logged-in");
    try {
      history.replaceState(null, "", location.href);
    } catch (_) {}
  }
})();
