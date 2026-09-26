namespace M.Services.Service
{
    /// <summary>
    /// Tiện ích xác định ca hành chính theo giờ Việt Nam (UTC+7)
    /// và đổi thời gian log chấm công về giờ VN để đánh giá đúng/trễ.
    /// </summary>
    public static class AttendanceStatusEvaluator
    {
        /// <summary>
        /// Ca hành chính theo giờ VN:
        /// 7h -> Ca sáng, 12h -> Ca chiều, 18h (6h tối) -> Ca tối.
        /// </summary>
        public static (TimeOnly Start, TimeOnly End, string Name) AdminShift(
            int vnHour)
        {
            if (vnHour >= 18 || vnHour < 5)
                return (new TimeOnly(18, 0), new TimeOnly(22, 0), "Ca tối");
            if (vnHour >= 12)
                return (new TimeOnly(12, 0), new TimeOnly(18, 0), "Ca chiều");
            return (new TimeOnly(7, 0), new TimeOnly(12, 0), "Ca sáng");
        }
    }
}
