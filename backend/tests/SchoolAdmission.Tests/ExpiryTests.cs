using System.Net;
using SchoolAdmission.Domain.Enums;
using SchoolAdmission.Tests.Support;

namespace SchoolAdmission.Tests;

[Collection("Api")]
public class ExpiryTests(ApiFixture api)
{
    private HttpClient Client => api.Client;

    [Fact]
    public async Task Expired_hold_shows_expired_frees_the_seat_and_cannot_be_confirmed()
    {
        var cls = await Client.CreateClassAsync(seats: 1);
        var a = TestIds.NewStudentId();
        var b = TestIds.NewStudentId();

        var holdA = await Client.ApplyOkAsync(cls.Id, a);
        var full = await Client.ApplyAsync(cls.Id, b);
        await full.AssertErrorAsync(HttpStatusCode.Conflict, "NO_SEATS_AVAILABLE");

        await api.ExpireHoldNowAsync(holdA.HoldId);

        // No API call was needed to release the seat.
        var view = await (await Client.GetHoldAsync(holdA.HoldId)).ReadAsync<HoldDto>();
        Assert.Equal("EXPIRED", view.Status);

        var detail = await Client.GetClassAsync(cls.Id);
        Assert.Equal(0, detail.HeldSeats);
        Assert.Equal(1, detail.AvailableSeats);

        var lateConfirm = await Client.ConfirmAsync(holdA.HoldId, a);
        await lateConfirm.AssertErrorAsync(HttpStatusCode.Conflict, "HOLD_EXPIRED");

        // The freed seat really is available to someone else.
        var holdB = await Client.ApplyOkAsync(cls.Id, b);
        Assert.Equal("HELD", holdB.Status);

        // And the stale expired row does not count as a duplicate: A just finds the class full.
        var retry = await Client.ApplyAsync(cls.Id, a);
        await retry.AssertErrorAsync(HttpStatusCode.Conflict, "NO_SEATS_AVAILABLE");
    }

    [Fact]
    public async Task Student_can_apply_again_after_their_own_hold_expired()
    {
        var cls = await Client.CreateClassAsync(seats: 2);
        var student = TestIds.NewStudentId();
        var first = await Client.ApplyOkAsync(cls.Id, student);

        await api.ExpireHoldNowAsync(first.HoldId);

        var second = await Client.ApplyOkAsync(cls.Id, student);
        Assert.NotEqual(first.HoldId, second.HoldId);
        Assert.Equal(1, (await Client.GetClassAsync(cls.Id)).HeldSeats);
    }

    [Fact]
    public async Task Background_job_marks_expired_holds_in_the_database()
    {
        var cls = await Client.CreateClassAsync(seats: 1);
        var hold = await Client.ApplyOkAsync(cls.Id, TestIds.NewStudentId());

        await api.ExpireHoldNowAsync(hold.HoldId);

        var tidied = await ApiFixture.WaitUntilAsync(
            async () => await api.GetStoredStatusAsync(hold.HoldId) == HoldStatus.Expired,
            TimeSpan.FromSeconds(10));

        Assert.True(tidied, "The expiry job did not mark the stale hold as Expired within 10 seconds.");
    }
}