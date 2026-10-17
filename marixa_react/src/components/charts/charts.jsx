import {
  ResponsiveContainer,
  PieChart,
  Pie,
  Cell,
  Tooltip,
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  LineChart,
  Line,
} from "recharts";

// Bảng màu nhất quán theo theme.css (đỏ = block, xanh = primary).
const CHART_COLORS = [
  "#1647C8", // primary
  "#00BFD0", // cyan
  "#F3B928", // gold
  "#B42318", // err (bị chặn / vắng)
  "#16a34a", // ok (hoàn thành)
  "#8A99AB", // muted
];

const tooltipStyle = {
  background: "#fff",
  border: "1px solid var(--th-border, #DCE3EC)",
  borderRadius: 10,
  fontSize: 13,
  color: "var(--th-text, #172033)",
  boxShadow: "0 8px 24px rgba(23,32,51,.10)",
};

// ===== Thẻ bọc chart: tiêu đề + nội dung + trạng thái trống =====
export const ChartCard = ({ title, hint, children, empty, emptyText = "Chưa có dữ liệu để hiển thị biểu đồ." }) => (
  <section className="mx-chart-card">
    <header className="mx-chart-card-head">
      <h3>{title}</h3>
      {hint ? <span className="att-muted">{hint}</span> : null}
    </header>
    {empty ? <p className="mx-chart-empty">{emptyText}</p> : <div className="mx-chart-body">{children}</div>}
  </section>
);

// ===== Donut: phân bố theo 1 danh mục =====
export const DonutChart = ({
  data,
  valueKey = "value",
  nameKey = "name",
  height = 210,
  centerValue = "",
  centerLabel = "",
  legend = true,
} = {}) => {
  const colorOf = (d, i) => d.color || CHART_COLORS[i % CHART_COLORS.length];
  return (
  <div className="mx-chart">
    <div className="mx-chart-donut" style={{ position: "relative" }}>
      <ResponsiveContainer width="100%" height={height}>
        <PieChart>
          <Pie
            data={data}
            dataKey={valueKey}
            nameKey={nameKey}
            innerRadius="60%"
            outerRadius="88%"
            paddingAngle={2}
            stroke="#fff"
            isAnimationActive
          >
            {data.map((d, i) => (
              <Cell key={i} fill={colorOf(d, i)} />
            ))}
          </Pie>
          <Tooltip contentStyle={tooltipStyle} />
        </PieChart>
      </ResponsiveContainer>
      {(centerValue || centerLabel) && (
        <div className="mx-chart-center">
          <strong>{centerValue}</strong>
          {centerLabel && <span>{centerLabel}</span>}
        </div>
      )}
    </div>
    {legend && (
      <ul className="mx-chart-legend">
        {data.map((d, i) => (
          <li key={i}>
            <i className="dot" style={{ background: colorOf(d, i) }} />
            <span>{d[nameKey]}</span>
            <b>{d[valueKey]}</b>
          </li>
        ))}
      </ul>
    )}
  </div>
  );
};

// ===== Cột dọc: giá trị theo tuần tự (vd giờ/ngày trong tháng) =====
export const ColumnChart = ({ data, xKey = "name", yKey = "value", height = 220, yTickFormatter } = {}) => (
  <ResponsiveContainer width="100%" height={height}>
    <BarChart data={data} margin={{ top: 8, right: 8, left: -12, bottom: 0 }} barSize={22}>
      <CartesianGrid vertical={false} stroke="#EEF2F6" />
      <XAxis dataKey={xKey} tick={{ fontSize: 11, fill: "var(--th-text-2, #526174)" }} tickLine={false} axisLine={false} interval="preserveStartEnd" />
      <YAxis tick={{ fontSize: 11, fill: "var(--th-text-2, #526174)" }} tickLine={false} axisLine={false} tickFormatter={yTickFormatter} width={40} />
      <Tooltip contentStyle={tooltipStyle} cursor={{ fill: "rgba(22,71,200,0.06)" }} />
      <Bar dataKey={yKey} fill="#1647C8" radius={[4, 4, 0, 0]} />
    </BarChart>
  </ResponsiveContainer>
);

// ===== Cột ngang: top N (vd nhân viên theo tổng giờ) =====
export const HBarChart = ({ data, xKey = "value", yKey = "name", height = 300, unit = "" } = {}) => (
  <ResponsiveContainer width="100%" height={height}>
    <BarChart data={data} layout="vertical" margin={{ top: 4, right: 16, left: 8, bottom: 0 }}>
      <CartesianGrid horizontal={false} stroke="#EEF2F6" />
      <XAxis type="number" tick={{ fontSize: 11, fill: "var(--th-text-2, #526174)" }} tickLine={false} axisLine={false} tickFormatter={(v) => (unit ? `${v}${unit}` : v)} />
      <YAxis
        type="category"
        dataKey={yKey}
        width={130}
        tick={{ fontSize: 12, fill: "var(--th-text, #172033)" }}
        tickLine={false}
        axisLine={false}
      />
      <Tooltip contentStyle={tooltipStyle} cursor={{ fill: "rgba(22,71,200,0.06)" }} />
      <Bar dataKey={xKey} fill="#00BFD0" radius={[0, 4, 4, 0]} barSize={16} />
    </BarChart>
  </ResponsiveContainer>
);

// ===== Đường gấp khúc: xu hướng theo thời gian =====
export const TrendLine = ({ data, xKey = "name", yKey = "value", height = 220, yTickFormatter } = {}) => (
  <ResponsiveContainer width="100%" height={height}>
    <LineChart data={data} margin={{ top: 8, right: 8, left: -12, bottom: 0 }}>
      <CartesianGrid vertical={false} stroke="#EEF2F6" />
      <XAxis dataKey={xKey} tick={{ fontSize: 11, fill: "var(--th-text-2, #526174)" }} tickLine={false} axisLine={false} />
      <YAxis tick={{ fontSize: 11, fill: "var(--th-text-2, #526174)" }} tickLine={false} axisLine={false} tickFormatter={yTickFormatter} width={40} allowDecimals={false} />
      <Tooltip contentStyle={tooltipStyle} />
      <Line type="monotone" dataKey={yKey} stroke="#1647C8" strokeWidth={2.5} dot={{ r: 4, fill: "#1647C8", strokeWidth: 0 }} activeDot={{ r: 5 }} />
    </LineChart>
  </ResponsiveContainer>
);
