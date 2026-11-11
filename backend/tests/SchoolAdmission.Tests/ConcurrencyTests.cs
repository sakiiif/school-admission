using System.Net;
using SchoolAdmission.Tests.Support;

namespace SchoolAdmission.Tests;

[Collection("Api")]
public class ConcurrencyTests(ApiFixture api)
{
    private HttpClient Client => api.Client;

    [Fact]
    public async Task Hundred_parallel_applications_never_exceed_ten_seats()
    {
        var cls = await Client.CreateClassAsync(seats: 10);

        // While the burst runs, keep reading the class and remember the highest
        // held + confirmed value ever observed: it must never pass totalSeats.
        using var stop = new CancellationTokenSource();
        var maxOccupied = 0;
        var poller = Task.Run(async () =>
        {
            while (!stop.IsCancellationRequested)
            {
                var snapshot = await Client.GetClassAsync(cls.Id);
                maxOccupied = Math.Max(maxOccupied, snapshot.HeldSeats + snapshot.ConfirmedSeats);
                await Task.Delay(5);
            }
        });

        var responses = await Burst.RunAsync(100,
            _ => Client.ApplyAsync(cls.Id, TestIds.NewStudentId()));

        stop.Cancel();
        await poller;

        Assert.Equal(10, responses.Count(r => r.StatusCode == HttpStatusCode.Created));
        Assert.Equal(90, responses.Count(r => r.StatusCode == HttpStatusCode.Conflict));
        Assert.True(maxOccupied <= 10, $"Observed {maxOccupied} occupied seats, limit is 10.");

        var final = await Client.GetClassAsync(cls.Id);
        Assert.Equal(10, final.HeldSeats);
        Assert.Equal(0, final.ConfirmedSeats);
        Assert.Equal(0, final.AvailableSeats);
    }

    [Fact]
    public async Task Same_student_applying_in_parallel_gets_exactly_one_seat()
    {
        var cls = await Client.CreateClassAsync(seats: 5);
        var student = TestIds.NewStudentId();

        var responses = await Burst.RunAsync(20, _ => Client.ApplyAsync(cls.Id, student));

        Assert.Equal(1, responses.Count(r => r.StatusCode == HttpStatusCode.Created));
        Assert.Equal(19, responses.Count(r => r.StatusCode == HttpStatusCode.Conflict));
        Assert.Equal(1, (await Client.GetClassAsync(cls.Id)).HeldSeats);
    }

    [Fact]
    public async Task Confirming_the_same_hold_twenty_times_in_parallel_counts_once()
    {
        var cls = await Client.CreateClassAsync(seats: 5);
        var student = TestIds.NewStudentId();
        var hold = await Client.ApplyOkAsync(cls.Id, student);

        var responses = await Burst.RunAsync(20, _ => Client.ConfirmAsync(hold.HoldId, student));

        Assert.All(responses, r => Assert.Equal(HttpStatusCode.OK, r.StatusCode));

        var final = await Client.GetClassAsync(cls.Id);
        Assert.Equal(1, final.ConfirmedSeats);
        Assert.Equal(0, final.HeldSeats);
        Assert.Equal(4, final.AvailableSeats);
    }
}