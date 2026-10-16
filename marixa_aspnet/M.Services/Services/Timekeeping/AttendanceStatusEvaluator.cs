namespace M.Services.Services.Timekeeping
{
    /// <summary>
    /// Tiện ích xác định ca hành chính chuẩn của Marixa
    /// (08:00 - 17:00) làm mốc mặc định khi nhân viên chưa được gán ca kế hoạch.
    /// </summary>
    public static class AttendanceStatusEvaluator
    {
        /// <summary>
        /// Ca hành chính duy nhất của Marixa: 08:00 - 17:00.
        /// </summary>
        public static (TimeOnly Start, TimeOnly End, string Name) AdminShift()
        {
            return (new TimeOnly(8, 0), new TimeOnly(17, 0), "Ca hành chính");
        }
    }
}
