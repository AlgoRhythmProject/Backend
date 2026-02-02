namespace AlgoRhythm.Data
{
    public static class SeedIds
    {
        // Users and roles
        public static readonly Guid AdminRoleId = new("c4149666-4e59-450a-b14a-57b1f5c355c7");
        public static readonly Guid StudentRoleId = new("2a1a2b72-f1d3-4a0b-9878-3a95c80a2d2f");
        public static readonly Guid AdminUserId = new("d0208b07-0051-4148-89c0-82a392825313");

        // Courses and lectures
        public static readonly Guid CourseId1 = new("c3f569d5-716d-4952-b91c-8b8393e18a0a");
        public static readonly Guid LectureId1 = new("a0f443b2-601e-450a-9d62-f947e62d4990");
        public static readonly Guid LectureId2 = new("b1d332c1-792f-4a1b-8c71-e058f71c4c31");
        public static readonly Guid LectureTextId1 = new("53e925c4-02f3-4247-a8b2-540161a0673d");

        // Tasks and test cases
        public static readonly Guid TaskId1 = new("d2c11a0b-331e-450a-8d51-f739e51c4a11");
        public static readonly Guid ProgrammingTaskId1 = new("e3b22b1c-442f-4a1b-9c62-e840f62d5b22");
        public static readonly Guid TestCaseId1 = new("f4d11c2d-553e-450a-8d51-f739e51c4a11");

        // Tags
        public static readonly Guid TagId1 = new("10a010b0-0001-0002-0003-000000000001");
        public static readonly Guid TagId2 = new("10a010b0-0001-0002-0003-000000000002");
    }
}
