import { useCallback, useEffect, useState } from "react";
import Header from "./Header";
import Sidebar from "./Sidebar";
import { usePillNavActiveInView } from "./usePillNav";
import "./header.css";

const COLLAPSE_KEY = "marixa_sidebar_collapsed";

// Shared authenticated layout: header, sidebar and content.
// The header spans the page; the sidebar can collapse.
const AppLayout = ({ profile, children }) => {
    usePillNavActiveInView();
    const [collapsed, setCollapsed] = useState(() => {
        try {
            return localStorage.getItem(COLLAPSE_KEY) === "1";
        } catch {
            return false;
        }
    });

    const toggle = useCallback(() => {
        setCollapsed((v) => {
            const next = !v;
            try {
                localStorage.setItem(COLLAPSE_KEY, next ? "1" : "0");
            } catch {
                // Storage can be unavailable in a restricted browser.
            }
            return next;
        });
    }, []);

    // Toggle with [ when the focus is outside an editable field.
    useEffect(() => {
        const onKey = (e) => {
            if (e.key === "[") toggle();
        };
        window.addEventListener("keydown", onKey);
        return () => window.removeEventListener("keydown", onKey);
    }, [toggle]);

    return (
        <div className="app-shell">
            <Header
                profile={profile}
                collapsed={collapsed}
                onToggleSidebar={toggle}
            />
            <div className="app-body">
                <Sidebar collapsed={collapsed} />
                <main className="att-main">
                    {children}
                </main>
            </div>
        </div>
    );
};

export default AppLayout;
