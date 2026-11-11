using System.Net;
using SchoolAdmission.Tests.Support;

namespace SchoolAdmission.Tests;

[Collection("Api")]
public class AdmissionRulesTests(ApiFixture api)
{
    private HttpClient Client => api.Client;

    [Fact]
    public async Task Created_class_and_hold_expose_the_contract_fields()
    {
        var cls = await Client.CreateClassAsync(seats: 120, name: "Class 6 - Morning Shift");
        Assert.False(string.IsNullOrWhiteSpace(cls.Id));
        Assert.Equal("Class 6 - Morning Shift", cls.Name);
        Assert.Equal(120, cls.TotalSeats);

        var student = TestIds.NewStudentId();
        var hold = await Client.ApplyOkAsync(cls.Id, student);
        Assert.False(string.IsNullOrWhiteSpace(hold.HoldId));
        Assert.Equal(cls.Id, hold.ClassId);
        Assert.Equal(student, hold.StudentId);
        Assert.Equal("HELD", hold.Status);
        Assert.Equal(DateTimeKind.Utc, hold.ExpiresAt.Kind);

        var fetched = await (await Client.GetHoldAsync(hold.HoldId)).ReadAsync<HoldDto>();
        Assert.Equal("HELD", fetched.Status);

        var detail = await Client.GetClassAsync(cls.Id);
        Assert.Equal(1, detail.HeldSeats);
        Assert.Equal(0, detail.ConfirmedSeats);
        Assert.Equal(119, detail.AvailableSeats);

        var list = await (await Client.GetAsync("/classes")).ReadAsync<List<ClassDto>>();
        Assert.Contains(list, c => c.Id == cls.Id && c.HeldSeats == 1);
    }

    [Fact]
    public async Task Same_student_cannot_hold_the_same_class_twice()
    {
        var cls = await Client.CreateClassAsync(seats: 5);
        var student = TestIds.NewStudentId();
        await Client.ApplyOkAsync(cls.Id, student);

        var second = await Client.ApplyAsync(cls.Id, student);

        await second.AssertErrorAsync(HttpStatusCode.Conflict, "DUPLICATE_ACTIVE_HOLD");
        Assert.Equal(1, (await Client.GetClassAsync(cls.Id)).HeldSeats);
    }

    [Fact]
    public async Task Confirmed_student_cannot_apply_again()
    {
        var cls = await Client.CreateClassAsync(seats: 5);
        var student = TestIds.NewStudentId();
        var hold = await Client.ApplyOkAsync(cls.Id, student);
        Assert.Equal(HttpStatusCode.OK, (await Client.ConfirmAsync(hold.HoldId, student)).StatusCode);

        var again = await Client.ApplyAsync(cls.Id, student);

        await again.AssertErrorAsync(HttpStatusCode.Conflict, "DUPLICATE_ACTIVE_HOLD");
    }

    [Fact]
    public async Task Only_the_owner_can_confirm_or_cancel_a_hold()
    {
        var cls = await Client.CreateClassAsync(seats: 5);
        var owner = TestIds.NewStudentId();
        var intruder = TestIds.NewStudentId();
        var hold = await Client.ApplyOkAsync(cls.Id, owner);

        var confirm = await Client.ConfirmAsync(hold.HoldId, intruder);
        await confirm.AssertErrorAsync(HttpStatusCode.Forbidden, "NOT_HOLD_OWNER");

        var cancel = await Client.CancelAsync(hold.HoldId, intruder);
        await cancel.AssertErrorAsync(HttpStatusCode.Forbidden, "NOT_HOLD_OWNER");

        var stillHeld = await (await Client.GetHoldAsync(hold.HoldId)).ReadAsync<HoldDto>();
        Assert.Equal("HELD", stillHeld.Status);
    }

    [Fact]
    public async Task Confirm_moves_the_seat_from_held_to_confirmed_and_is_idempotent()
    {
        var cls = await Client.CreateClassAsync(seats: 3);
        var student = TestIds.NewStudentId();
        var hold = await Client.ApplyOkAsync(cls.Id, student);

        for (var i = 0; i < 3; i++)
        {
            var response = await Client.ConfirmAsync(hold.HoldId, student);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.ReadAsync<ConfirmDto>();
            Assert.Equal("CONFIRMED", body.Status);
        }

        var detail = await Client.GetClassAsync(cls.Id);
        Assert.Equal(0, detail.HeldSeats);
        Assert.Equal(1, detail.ConfirmedSeats);
        Assert.Equal(2, detail.AvailableSeats);
    }

    [Fact]
    public async Task Cancel_releases_the_seat_and_allows_applying_again()
    {
        var cls = await Client.CreateClassAsync(seats: 1);
        var student = TestIds.NewStudentId();
        var other = TestIds.NewStudentId();
        var hold = await Client.ApplyOkAsync(cls.Id, student);

        (await Client.ApplyAsync(cls.Id, other)).AssertErrorAsync(HttpStatusCode.Conflict, "NO_SEATS_AVAILABLE").GetAwaiter().GetResult();

        Assert.Equal(HttpStatusCode.NoContent, (await Client.CancelAsync(hold.HoldId, student)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await Client.CancelAsync(hold.HoldId, student)).StatusCode); // idempotent

        var cancelled = await (await Client.GetHoldAsync(hold.HoldId)).ReadAsync<HoldDto>();
        Assert.Equal("CANCELLED", cancelled.Status);
        Assert.Equal(1, (await Client.GetClassAsync(cls.Id)).AvailableSeats);

        // The partial unique index ignores cancelled rows, so the same student can apply again.
        var reapplied = await Client.ApplyOkAsync(cls.Id, student);
        Assert.Equal("HELD", reapplied.Status);
    }

    [Fact]
    public async Task Cancelled_hold_cannot_be_confirmed_and_confirmed_admission_cannot_be_cancelled()
    {
        var cls = await Client.CreateClassAsync(seats: 5);
        var a = TestIds.NewStudentId();
        var b = TestIds.NewStudentId();

        var holdA = await Client.ApplyOkAsync(cls.Id, a);
        await Client.CancelAsync(holdA.HoldId, a);
        var confirmCancelled = await Client.ConfirmAsync(holdA.HoldId, a);
        await confirmCancelled.AssertErrorAsync(HttpStatusCode.Conflict, "INVALID_HOLD_STATE");

        var holdB = await Client.ApplyOkAsync(cls.Id, b);
        await Client.ConfirmAsync(holdB.HoldId, b);
        var cancelConfirmed = await Client.CancelAsync(holdB.HoldId, b);
        await cancelConfirmed.AssertErrorAsync(HttpStatusCode.Conflict, "INVALID_HOLD_STATE");
    }

    [Fact]
    public async Task My_holds_lists_only_the_students_own_applications_newest_first()
    {
        var morning = await Client.CreateClassAsync(seats: 5, name: "Class 6 - Morning Shift");
        var day = await Client.CreateClassAsync(seats: 5, name: "Class 6 - Day Shift");
        var a = TestIds.NewStudentId();
        var b = TestIds.NewStudentId();

        var first = await Client.ApplyOkAsync(morning.Id, a);
        var second = await Client.ApplyOkAsync(day.Id, a);
        var others = await Client.ApplyOkAsync(morning.Id, b);

        var mine = await (await Client.MyHoldsAsync(a)).ReadAsync<List<MyHoldDto>>();

        Assert.Equal(2, mine.Count);
        Assert.Equal(second.HoldId, mine[0].HoldId);
        Assert.Equal("Class 6 - Day Shift", mine[0].ClassName);
        Assert.Equal(first.HoldId, mine[1].HoldId);
        Assert.Equal("Class 6 - Morning Shift", mine[1].ClassName);
        Assert.DoesNotContain(mine, h => h.HoldId == others.HoldId);
    }

    [Fact]
    public async Task Unknown_class_and_unknown_hold_return_404()
    {
        var student = TestIds.NewStudentId();

        await (await Client.GetAsync("/classes/c_doesnotexist"))
            .AssertErrorAsync(HttpStatusCode.NotFound, "CLASS_NOT_FOUND");
        await (await Client.ApplyAsync("c_doesnotexist", student))
            .AssertErrorAsync(HttpStatusCode.NotFound, "CLASS_NOT_FOUND");
        await (await Client.GetHoldAsync("h_doesnotexist"))
            .AssertErrorAsync(HttpStatusCode.NotFound, "HOLD_NOT_FOUND");
        await (await Client.ConfirmAsync("h_doesnotexist", student))
            .AssertErrorAsync(HttpStatusCode.NotFound, "HOLD_NOT_FOUND");
    }
}