import AdminAppLayout from "../admin/layout/AdminAppLayout";
import { useState, useEffect, useCallback, useRef } from "react";
import HrAppLayout from "../hr/layout/HrAppLayout";
import axiosClient from "../../services/api/axiosClient";
import { getAllPages } from "../../services/api/lists";
import { itemsOf, apiErrorMessage } from "../../services/api/contracts";
import ResponsiveTable from "../../components/common/ResponsiveTable";
import { catalogs, recordPayload } from "./catalogSchema";
import "./catalog.css";
export default function CatalogPage({ contracts = false, admin = false }) {
  const Layout = admin ? AdminAppLayout : HrAppLayout;
  const [kind, setKind] = useState(
    contracts ? "EmployeeContract" : "Department",
  );
  const schema = catalogs[kind];
  const [rows, setRows] = useState([]),
    [employees, setEmployees] = useState([]),
    [query, setQuery] = useState(""),
    [status, setStatus] = useState(""),
    [page, setPage] = useState(1);
  const [loading, setLoading] = useState(true),
    [error, setError] = useState(""),
    [busy, setBusy] = useState(false),
    [notice, setNotice] = useState("");
  const [form, setForm] = useState(null),
    [reason, setReason] = useState(""),
    [history, setHistory] = useState(null);
  const dialog = useRef(null);
  const trigger = useRef(null);
  const generation = useRef(0);
  const load = useCallback(async () => {
    const request = ++generation.current;
    setLoading(true);
    setError("");
    setPage(1);
    try {
      const r = await getAllPages(
        "/" + kind + "/" + (status === "archived" ? "get-archived" : "get-all"),
      );
      if (request !== generation.current) return;
      setRows(itemsOf(r));
      if (kind === "EmployeeContract" || kind === "Department")
        setEmployees(itemsOf(await getAllPages("/Employee/get-all")));
    } catch (e) {
      if (request === generation.current) setError(apiErrorMessage(e));
    } finally {
      if (request === generation.current) setLoading(false);
    }
  }, [kind, status]);
  useEffect(() => {
    load();
  }, [load]);
  const dialogOpen = !!form || !!history;
  useEffect(() => {
    if (dialogOpen) {
      const modal = dialog.current;
      const previous = trigger.current;
      modal?.showModal();
      return () => {
        modal?.close();
        previous?.focus();
      };
    }
  }, [dialogOpen]);
  const overlaps =
    contracts && form?.startDate
      ? rows.filter(
          (r) =>
            r.id !== form.id &&
            r.employeeId === form.employeeId &&
            r.startDate?.slice(0, 10) <= (form.endDate || "9999-12-31") &&
            (!r.endDate || r.endDate.slice(0, 10) >= form.startDate),
        )
      : [];
  const filtered = rows.filter(
    (r) =>
      (!status || status === "archived" || String(r.isActive) === status) &&
      JSON.stringify([r.name, r.code, r.employeeName, r.contractNumber, r.date])
        .toLocaleLowerCase("vi")
        .includes(query.toLocaleLowerCase("vi")),
  );
  const start = (row, event) => {
    trigger.current = event.currentTarget;
    setReason("");
    setError("");
    setForm(
      row
        ? { ...row }
        : Object.fromEntries(
            schema.fields
              .map(([k, , t]) => [
                k,
                t === "checkbox"
                  ? true
                  : t === "holiday" || t === "contract"
                    ? 2
                    : "",
              ])
              .concat([["isActive", true]]),
          ),
    );
  };
  const save = async (event) => {
    event.preventDefault();
    setBusy(true);
    setError("");
    try {
      const payload = recordPayload(kind, form);
      await axiosClient[form.id ? "put" : "post"](
        "/" + kind + "/" + (form.id ? "update" : "create"),
        payload,
        {
          headers: {
            "X-Change-Reason": encodeURIComponent(reason),
            "X-Record-Version": form.lastUpdatedTime || "",
          },
        },
      );
      setForm(null);
      setNotice("Đã lưu thay đổi và lịch sử.");
      await load();
    } catch (e) {
      setError(apiErrorMessage(e));
    } finally {
      setBusy(false);
    }
  };
  const showHistory = async (row, event) => {
    trigger.current = event.currentTarget;
    setError("");
    try {
      setHistory(
        itemsOf(
          await getAllPages("/AuditLog/query", {
            entityType: kind,
            entityId: row.id,
          }),
        ),
      );
    } catch (e) {
      setError(apiErrorMessage(e));
    }
  };
  const archive = async () => {
    if (!reason.trim() || busy) return;
    setBusy(true);
    try {
      await axiosClient.delete("/" + kind + "/soft-delete/" + form.id, {
        headers: {
          "X-Change-Reason": encodeURIComponent(reason),
          "X-Record-Version": form.lastUpdatedTime,
        },
      });
      setForm(null);
      setNotice("Đã lưu trữ bản ghi.");
      await load();
    } catch (e) {
      setError(apiErrorMessage(e));
    } finally {
      setBusy(false);
    }
  };
  return (
    <Layout>
      <div className="erp-page">
        <section className="erp-banner">
          <div>
            <span className="erp-eyebrow">MARIXA / NHÂN SỰ</span>
            <h1>{contracts ? "Hợp đồng lao động" : "Dữ liệu nền tảng"}</h1>
            <p>
              {contracts
                ? "Quản lý thông tin hợp đồng trong một không gian."
                : "Danh mục thống nhất cho mọi hồ sơ trong tổ chức."}
            </p>
          </div>
          <span className="erp-banner-mark" aria-hidden="true">
            {contracts ? "↗" : "▦"}
          </span>
        </section>
        {!contracts && (
          <nav className="erp-tabs" aria-label="Nhóm danh mục">
            {Object.entries(catalogs)
              .filter(([k]) => k !== "EmployeeContract")
              .map(([k, s]) => (
                <button
                  key={k}
                  aria-pressed={kind === k}
                  onClick={() => {
                    setKind(k);
                    setPage(1);
                    setQuery("");
                    setStatus("");
                  }}
                >
                  {s.title}
                </button>
              ))}
          </nav>
        )}
        <section className="erp-panel">
          <div className="erp-section-head">
            <div>
              <span className="erp-eyebrow">
                {loading ? "ĐANG TẢI" : rows.length + " BẢN GHI"}
              </span>
              <h2>{schema.title}</h2>
              <p>{schema.subtitle}</p>
            </div>
            <button className="erp-primary" onClick={(e) => start(null, e)}>
              + Thêm mới
            </button>
          </div>
          <div className="erp-toolbar">
            <label>
              Tìm kiếm
              <input
                type="search"
                value={query}
                onChange={(e) => {
                  setQuery(e.target.value);
                  setPage(1);
                }}
                placeholder="Nhập mã hoặc tên…"
              />
            </label>
            {
              <label>
                Trạng thái
                <select
                  value={status}
                  onChange={(e) => {
                    setStatus(e.target.value);
                    setPage(1);
                  }}
                >
                  <option value="">Tất cả</option>
                  {!contracts && (
                    <>
                      <option value="true">Đang sử dụng</option>
                      <option value="false">Ngừng sử dụng</option>
                    </>
                  )}
                  <option value="archived">Đã lưu trữ</option>
                </select>
              </label>
            }
            <button onClick={load} disabled={loading}>
              Tải lại
            </button>
          </div>
          {notice && <p role="status">{notice}</p>}
          {error && (
            <p role="alert" className="erp-error">
              {error}
            </p>
          )}
          {loading ? (
            <p role="status">Đang tải dữ liệu…</p>
          ) : (
            <ResponsiveTable>
              <thead>
                <tr>
                  <th>Mã / Số</th>
                  <th>{contracts ? "Nhân viên" : "Tên danh mục"}</th>
                  <th>{contracts ? "Thời hạn" : "Trạng thái"}</th>
                  <th>Thao tác</th>
                </tr>
              </thead>
              <tbody>
                {filtered.slice((page - 1) * 15, page * 15).map((r) => (
                  <tr key={r.id}>
                    <td>
                      {r.code ||
                        r.contractNumber ||
                        r.date?.slice(0, 10) ||
                        "—"}
                    </td>
                    <td>
                      {r.name ||
                        r.employeeName ||
                        employees.find((e) => e.id === r.employeeId)
                          ?.fullName ||
                        "—"}
                    </td>
                    <td>
                      {contracts ? (
                        r.startDate?.slice(0, 10) +
                        " → " +
                        (r.endDate?.slice(0, 10) || "Không xác định")
                      ) : (
                        <span className="erp-status">
                          {r.isActive ? "Đang sử dụng" : "Ngừng sử dụng"}
                        </span>
                      )}
                    </td>
                    <td>
                      {status !== "archived" && (
                        <button onClick={(e) => start(r, e)}>Chỉnh sửa</button>
                      )}{" "}
                      <button onClick={(e) => showHistory(r, e)}>
                        Lịch sử
                      </button>
                    </td>
                  </tr>
                ))}
                {!filtered.length && (
                  <tr>
                    <td colSpan="4">Không có dữ liệu phù hợp.</td>
                  </tr>
                )}
              </tbody>
            </ResponsiveTable>
          )}
          <div className="erp-pagination">
            <span>
              {filtered.length} kết quả · Trang {page}
            </span>
            <button disabled={page === 1} onClick={() => setPage(page - 1)}>
              Trước
            </button>
            <button
              disabled={page * 15 >= filtered.length}
              onClick={() => setPage(page + 1)}
            >
              Sau
            </button>
          </div>
        </section>
        <dialog
          ref={dialog}
          className="erp-dialog"
          onCancel={(e) => {
            if (busy) e.preventDefault();
            else {
              setForm(null);
              setHistory(null);
            }
          }}
        >
          {form && (
            <form onSubmit={save}>
              <div className="erp-section-head">
                <h2>
                  {form.id ? "Chỉnh sửa" : "Thêm"} {schema.title.toLowerCase()}
                </h2>
                <button
                  type="button"
                  disabled={busy}
                  onClick={() => setForm(null)}
                  aria-label="Đóng"
                >
                  ×
                </button>
              </div>
              <div className="erp-form-grid">
                {schema.fields.map(([key, label, type = "text"]) => (
                  <label key={key}>
                    {label}
                    {["employee", "holiday", "contract"].includes(type) ? (
                      <select
                        required={key === "employeeId" || type !== "employee"}
                        disabled={!!form.id && key === "employeeId"}
                        value={form[key] ?? ""}
                        onChange={(e) =>
                          setForm({ ...form, [key]: e.target.value })
                        }
                      >
                        <option value="">Chọn…</option>
                        {(type === "employee"
                          ? employees.map((e) => [e.id, e.fullName])
                          : type === "holiday"
                            ? [
                                [1, "Ngày lễ"],
                                [2, "Lễ quốc gia"],
                                [3, "Làm bù"],
                                [4, "Nghỉ đặc biệt"],
                              ]
                            : [
                                [1, "Thử việc"],
                                [2, "Có thời hạn"],
                                [3, "Không thời hạn"],
                                [4, "Thời vụ"],
                              ]
                        ).map(([v, t]) => (
                          <option key={v} value={v}>
                            {t}
                          </option>
                        ))}
                      </select>
                    ) : (
                      <input
                        type={type}
                        step={type === "number" ? "0.01" : undefined}
                        min={type === "number" ? 0 : undefined}
                        required={[
                          "code",
                          "name",
                          "contractNumber",
                          "startDate",
                          "date",
                        ].includes(key)}
                        maxLength={
                          key === "code"
                            ? 50
                            : key === "contractNumber"
                              ? 100
                              : 150
                        }
                        checked={type === "checkbox" ? !!form[key] : undefined}
                        value={
                          type === "checkbox"
                            ? undefined
                            : type === "date"
                              ? form[key]?.slice(0, 10) || ""
                              : (form[key] ?? "")
                        }
                        onChange={(e) =>
                          setForm({
                            ...form,
                            [key]:
                              type === "checkbox"
                                ? e.target.checked
                                : e.target.value,
                          })
                        }
                      />
                    )}
                  </label>
                ))}
                {!contracts && (
                  <label>
                    <input
                      type="checkbox"
                      checked={form.isActive}
                      onChange={(e) =>
                        setForm({ ...form, isActive: e.target.checked })
                      }
                    />{" "}
                    Đang sử dụng
                  </label>
                )}
              </div>
              {!!overlaps.length && (
                <p role="status" className="erp-help">
                  Có {overlaps.length} hợp đồng chồng lấn thời gian. Bạn vẫn có
                  thể lưu.
                </p>
              )}
              {contracts && (
                <p className="erp-help">
                  Sửa hồi tố không tự tính lại lương/công. Kiểm tra các hợp đồng
                  chồng lấn trước khi lưu.
                </p>
              )}
              <label>
                Lý do thay đổi
                <textarea
                  required
                  minLength={5}
                  maxLength={500}
                  value={reason}
                  onChange={(e) => setReason(e.target.value)}
                />
              </label>
              {error && (
                <p role="alert" className="erp-error">
                  {error}
                </p>
              )}
              <div className="erp-dialog-actions">
                {form.id && (
                  <button
                    type="button"
                    disabled={busy || reason.trim().length < 5}
                    onClick={archive}
                  >
                    Lưu trữ
                  </button>
                )}
                <button
                  type="button"
                  disabled={busy}
                  onClick={() => setForm(null)}
                >
                  Hủy
                </button>
                <button className="erp-primary" disabled={busy}>
                  {busy ? "Đang lưu…" : "Lưu thay đổi"}
                </button>
              </div>
            </form>
          )}
          {history && (
            <section>
              <div className="erp-section-head">
                <h2>Lịch sử thay đổi</h2>
                <button onClick={() => setHistory(null)} aria-label="Đóng">
                  ×
                </button>
              </div>
              {!history.length && <p>Chưa có lịch sử được ghi nhận.</p>}
              {history.map((h) => (
                <article className="erp-history" key={h.id}>
                  <strong>{h.actorUserName || "Hệ thống"}</strong>
                  <p>
                    {new Date(h.createdTime).toLocaleString("vi-VN")} ·{" "}
                    {h.reason}
                  </p>
                  <details>
                    <summary>Xem trước / sau</summary>
                    <pre>{h.beforeJson || "Bản ghi mới"}</pre>
                    <pre>{h.afterJson}</pre>
                  </details>
                </article>
              ))}
            </section>
          )}
        </dialog>
      </div>
    </Layout>
  );
}
