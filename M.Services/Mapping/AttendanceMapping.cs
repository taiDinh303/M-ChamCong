using M.Contract.Repositories.Entities;
using ModelViews.AttendanceModelView;

namespace M.Services.Mappings
{
    public static class AttendanceMapping
    {
        // Mapping Entity -> AttendanceResponseModelView
        public static AttendanceResponseModelView ToViewModel(this Attendance? entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            var model = new AttendanceResponseModelView
            {
                Id = entity.Id,
                EmployeeId = entity.EmployeeId,
                EmployeeCode = entity.Employee?.EmployeeCode,
                EmployeeName = entity.Employee?.FullName,
                AttendanceDate = entity.AttendanceDate,
                Status = entity.Status,
                Note = entity.Note,
                CreatedTime = entity.CreatedTime,
                LastUpdatedTime = entity.LastUpdatedTime
            };

            return model;
        }

        // Mapping AttendanceResponseModelView -> Entity
        public static Attendance ToEntity(this AttendanceResponseModelView model)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));

            var entity = new Attendance
            {
                Id = model.Id,
                EmployeeId = model.EmployeeId,
                AttendanceDate = model.AttendanceDate,
                Status = model.Status,
                Note = model.Note,
                CreatedTime = model.CreatedTime,
                LastUpdatedTime = model.LastUpdatedTime
            };

            return entity;
        }

        // Mapping CreateAttendanceModelView -> Entity
        public static Attendance ToEntity(this CreateAttendanceModelView model)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));

            var entity = new Attendance
            {
                EmployeeId = model.EmployeeId,
                AttendanceDate = model.AttendanceDate,
                Status = model.Status,
                Note = model.Note
            };

            return entity;
        }

        // Mapping UpdateAttendanceModelView -> Entity
        public static void ToEntity(
            this UpdateAttendanceModelView model,
            Attendance entity)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            entity.EmployeeId = model.EmployeeId;
            entity.AttendanceDate = model.AttendanceDate;
            entity.Status = model.Status;

            entity.Note = model.Note == string.Empty
                ? string.Empty
                : model.Note == null
                    ? entity.Note
                    : model.Note;
        }
    }
}