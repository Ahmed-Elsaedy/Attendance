namespace Attendance.Web.DTOs
{
    public enum SessionAttendanceCodeValidationEnum
    {
        InvalidCode = 1,
        ExpiredCode = 2,
        SessionNotFound = 3,
        SessionEnded = 4,
        SessionNotStartedYet = 5,
        UnProcessable = 6,
        MultipleFacesDetected = 7,
        NoFacesDetected = 8,
        UnRecognizedPerson = 9,
        StudentDataCouldNotBeFound = 10,
        AlreadyRegisteredForThisSession = 11,
        Success = 12
    }
}
