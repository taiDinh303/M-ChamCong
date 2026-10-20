import { guidanceFor } from "./routeGuidance";

export default function PageGuidance({ pathname }) {
    const guide = guidanceFor(pathname);
    if (!guide) return null;
    return <aside className="mx-page-guide" aria-label="Hướng dẫn sử dụng trang">
        <div className="mx-page-guide-icon" aria-hidden="true">i</div>
        <div><h2>{guide.title}</h2><ul>{guide.steps.map((step) => <li key={step}>{step}</li>)}</ul>
        </div>
    </aside>;
}
