(() => {
  if (window.__sageLoginBound) return;
  window.__sageLoginBound = true;

  const ensureOverlay = () => {
    let overlay = document.getElementById("login-entering-portal");
    if (overlay) return overlay;

    const source = document.getElementById("login-entering");
    if (!source) return null;

    overlay = source.cloneNode(true);
    overlay.id = "login-entering-portal";
    document.body.appendChild(overlay);
    return overlay;
  };

  const showEntering = () => {
    const overlay = ensureOverlay();
    if (!overlay) return;
    document.documentElement.classList.add("sage-logging-in");
    overlay.classList.add("is-active");
    overlay.setAttribute("aria-hidden", "false");
    sessionStorage.setItem("sage-just-logged-in", "1");
  };

  const hideEntering = () => {
    document.documentElement.classList.remove("sage-logging-in");
    const overlay = document.getElementById("login-entering-portal");
    if (!overlay) return;
    overlay.classList.remove("is-active");
    overlay.setAttribute("aria-hidden", "true");
  };

  document.addEventListener(
    "submit",
    (event) => {
      const form = event.target;
      if (!(form instanceof HTMLFormElement)) return;
      if (!form.closest(".account-card")) return;
      showEntering();
    },
    true
  );

  // Si Blazor vuelve a pintar el login con error, soltar el overlay.
  document.addEventListener("DOMContentLoaded", () => {
    if (document.querySelector(".account-card .validation-message, .account-card .text-danger, .status-message")) {
      // no-op: el flag solo se limpia si hay mensaje tras post fallido
    }
  });

  window.addEventListener("pageshow", (event) => {
    if (event.persisted) hideEntering();
  });

  window.__sageHideLoginEntering = hideEntering;
})();
