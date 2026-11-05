(() => {
  if (sessionStorage.getItem("sage-just-logged-in") !== "1") return;
  sessionStorage.removeItem("sage-just-logged-in");
  document.documentElement.classList.add("sage-enter");
  window.setTimeout(() => document.documentElement.classList.remove("sage-enter"), 900);
})();
