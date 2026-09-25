import EntityDetailPage from "../../attendance/components/EntityDetailPage";

const BankPage = () => (
    <EntityDetailPage
        kind="bank"
        title="Tài khoản ngân hàng"
        subtitle="Thông tin tài khoản nhận lương của bạn"
        emptyMessage="Chưa có tài khoản ngân hàng."
        columns={[
            { key: "bankName", label: "Ngân hàng", render: (r) => r.bankName || "—" },
            { key: "accountNumber", label: "Số tài khoản" },
            { key: "accountHolderName", label: "Chủ tài khoản", render: (r) => r.accountHolderName || "—" },
            { key: "isPrimary", label: "Loại", render: (r) => (r.isPrimary ? "Chính" : "Phụ") },
        ]}
    />
);

export default BankPage;
