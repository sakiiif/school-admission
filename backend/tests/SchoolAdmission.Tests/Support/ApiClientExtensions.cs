using System.Net;
using System.Net.Http.Json;

namespace SchoolAdmission.Tests.Support;

public static class ApiClientExtensions
{
    public static async Task<ClassDto> CreateClassAsync(
        this HttpClient client, int seats, string name = "Class 6 - Morning Shift")
    {
        var response = await client.PostAsJsonAsync("/classes", new { name, totalSeats = seats });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await response.ReadAsync<ClassDto>();
    }

    public static async Task<ClassDto> GetClassAsync(this HttpClient client, string classId)
    {
        var response = await client.GetAsync($"/classes/{classId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.ReadAsync<ClassDto>();
    }

    public static Task<HttpResponseMessage> ApplyAsync(this HttpClient client, string classId, string? studentId) =>
        client.SendAsStudentAsync(HttpMethod.Post, $"/classes/{classId}/holds", studentId);

    public static async Task<HoldDto> ApplyOkAsync(this HttpClient client, string classId, string studentId)
    {
        var response = await client.ApplyAsync(classId, studentId);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await response.ReadAsync<HoldDto>();
    }

    public static Task<HttpResponseMessage> ConfirmAsync(this HttpClient client, string holdId, string? studentId) =>
        client.SendAsStudentAsync(HttpMethod.Post, $"/holds/{holdId}/confirm", studentId);

    public static Task<HttpResponseMessage> CancelAsync(this HttpClient client, string holdId, string? studentId) =>
        client.SendAsStudentAsync(HttpMethod.Delete, $"/holds/{holdId}", studentId);

    public static Task<HttpResponseMessage> GetHoldAsync(this HttpClient client, string holdId) =>
        client.GetAsync($"/holds/{holdId}");

    public static Task<HttpResponseMessage> MyHoldsAsync(this HttpClient client, string? studentId) =>
        client.SendAsStudentAsync(HttpMethod.Get, "/me/holds", studentId);

    private static Task<HttpResponseMessage> SendAsStudentAsync(
        this HttpClient client, HttpMethod method, string url, string? studentId)
    {
        var request = new HttpRequestMessage(method, url);
        if (studentId is not null)
            request.Headers.Add("X-Student-Id", studentId);
        return client.SendAsync(request);
    }

    public static async Task<T> ReadAsync<T>(this HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<T>())!;

    // Asserts the status code and the machine-readable error code in the JSON body
    public static async Task AssertErrorAsync(
        this HttpResponseMessage response, HttpStatusCode status, string errorCode)
    {
        Assert.Equal(status, response.StatusCode);
        var error = await response.ReadAsync<ErrorDto>();
        Assert.Equal(errorCode, error.Error);
        Assert.False(string.IsNullOrWhiteSpace(error.Message));
    }
}

public static class TestIds
{
    private static long _counter;

    // A unique valid 17-digit student registration number
    public static string NewStudentId() => $"2015269{Interlocked.Increment(ref _counter):D10}";
}

public static class Burst
{
    // Runs <paramref name="count"/> requests that all start at the same moment
    // (released together from a gate) to maximise contention.
    public static async Task<HttpResponseMessage[]> RunAsync(
        int count, Func<int, Task<HttpResponseMessage>> action)
    {
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var tasks = Enumerable.Range(0, count)
            .Select(i => Task.Run(async () =>
            {
                await gate.Task;
                return await action(i);
            }))
            .ToArray();

        gate.SetResult();
        return await Task.WhenAll(tasks);
    }
}