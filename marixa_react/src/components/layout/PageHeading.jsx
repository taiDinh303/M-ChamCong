import { useContext } from "react";
import { createPortal } from "react-dom";
import { PageHeadingContext } from "./PageHeadingContext";

export default function PageHeading({ icon, title, description }) {
    const target = useContext(PageHeadingContext);
    const heading = <div className="page-heading-content">
        {icon && <span className="page-heading-icon" aria-hidden="true">{icon}</span>}
        <div><h1>{title}</h1>{description && <p>{description}</p>}</div>
    </div>;
    return target === undefined ? heading : target ? createPortal(heading, target) : null;
}
