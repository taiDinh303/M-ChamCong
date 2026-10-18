import { useEffect, useRef, useState, useSyncExternalStore } from "react";
import { useLocation } from "react-router-dom";
import Header from "./Header";
import { PageHeadingContext } from "./PageHeadingContext";
import { pageTitle } from "./pageTitles";
import "./shell.css";

const mobileQuery = "(max-width: 768px)";
const subscribeMobile = (notify) => {
    const media = window.matchMedia(mobileQuery);
    media.addEventListener("change", notify);
    return () => media.removeEventListener("change", notify);
};
const isMobile = () => window.matchMedia(mobileQuery).matches;

export default function AuthenticatedShell({ storageKey, sidebar, children, profile }) {
    const mobile = useSyncExternalStore(subscribeMobile, isMobile, () => false);
    const { pathname } = useLocation();
    const [headingTarget, setHeadingTarget] = useState(null);
    const [drawerOpen, setDrawerOpen] = useState(false);
    const [collapsed, setCollapsed] = useState(() => {
        try { return localStorage.getItem(storageKey) === "1"; } catch { return false; }
    });
    const dialogRef = useRef(null);
    const toggleRef = useRef(null);

    const toggle = () => {
        if (mobile) setDrawerOpen((value) => !value);
        else setCollapsed((value) => {
            const next = !value;
            try { localStorage.setItem(storageKey, next ? "1" : "0"); } catch { /* optional preference */ }
            return next;
        });
    };

    useEffect(() => {
        const dialog = dialogRef.current;
        if (!mobile || !drawerOpen || !dialog) return;
        const toggleButton = toggleRef.current;
        const previousOverflow = document.body.style.overflow;
        document.body.style.overflow = "hidden";
        dialog.showModal();
        return () => {
            dialog.close();
            document.body.style.overflow = previousOverflow;
            toggleButton?.focus();
        };
    }, [mobile, drawerOpen]);

    const closeAfterNavigation = (event) => {
        if (event.target.closest("a[href], .att-nav-item, .work-back-btn")) setDrawerOpen(false);
    };

    return <PageHeadingContext.Provider value={headingTarget}>
        <div className="app-shell app-shell--responsive">
            <Header profile={profile} onToggleSidebar={toggle} toggleRef={toggleRef}
                sidebarExpanded={mobile ? drawerOpen : !collapsed} sidebarControls={mobile ? "mobile-navigation" : "desktop-navigation"}
                pageHeading={<div className="app-page-heading" ref={setHeadingTarget}>
                    <h1 className="page-heading-fallback">{pageTitle(pathname)}</h1>
                </div>} />
            <div className="app-body">
                {!mobile && <div id="desktop-navigation" className="desktop-navigation">{sidebar(collapsed)}</div>}
                <main className="att-main">{children}</main>
            </div>
            {mobile && <dialog ref={dialogRef} id="mobile-navigation" className="navigation-drawer" aria-labelledby="navigation-title"
                onCancel={() => setDrawerOpen(false)} onClick={(event) => {
                    if (event.target === event.currentTarget) setDrawerOpen(false);
                }}>
                <div className="navigation-drawer-panel">
                    <div className="navigation-drawer-head"><strong id="navigation-title">Điều hướng</strong>
                        <button type="button" aria-label="Đóng menu điều hướng" onClick={() => setDrawerOpen(false)}>×</button>
                    </div>
                    <div onClick={closeAfterNavigation}>{sidebar(false)}</div>
                </div>
            </dialog>}
        </div>
    </PageHeadingContext.Provider>;
}
