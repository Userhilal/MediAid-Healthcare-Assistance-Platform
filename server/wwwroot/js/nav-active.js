(function () {
    const currentPath = window.location.pathname.toLowerCase().replace(/\/$/, "") || "/";

    document.querySelectorAll("a.premium-nav-link").forEach((link) => {
        const url = new URL(link.href);
        const linkPath = url.pathname.toLowerCase().replace(/\/$/, "") || "/";

        if (linkPath !== "/" && (currentPath === linkPath || currentPath.startsWith(linkPath + "/"))) {
            link.classList.add("active");
        }

        if (currentPath === "/" && linkPath === "/") {
            link.classList.add("active");
        }
    });
})();

