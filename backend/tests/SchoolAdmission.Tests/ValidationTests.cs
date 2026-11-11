using System.Net;
using System.Text;
using SchoolAdmission.Tests.Support;

namespace SchoolAdmission.Tests;

[Collection("Api")]
public class ValidationTests(ApiFixture api)
{
    private HttpClient Client => api.Client;

    [Theory]
    [InlineData("123")]
    [InlineData("2015269123456789")]        // 16 digits
    [InlineData("201526912345678901")]      // 18 digits
    [InlineData("2015269123456789a")]       // contains a letter
    public async Task Malformed_student_id_is_rejected_with_400(string studentId)
    {
        var cls = await Client.CreateClassAsync(seats: 5);

        var response = await Client.ApplyAsync(cls.Id, studentId);

        await response.AssertErrorAsync(HttpStatusCode.BadRequest, "VALIDATION_ERROR");
        Assert.Equal(0, (await Client.GetClassAsync(cls.Id)).HeldSeats);
    }

    [Fact]
    public async Task Missing_student_id_header_is_rejected_with_400()
    {
        var cls = await Client.CreateClassAsync(seats: 5);

        await (await Client.ApplyAsync(cls.Id, studentId: null))
            .AssertErrorAsync(HttpStatusCode.BadRequest, "VALIDATION_ERROR");
        await (await Client.MyHoldsAsync(studentId: null))
            .AssertErrorAsync(HttpStatusCode.BadRequest, "VALIDATION_ERROR");
    }

    [Theory]
    [InlineData("""{"name":"","totalSeats":10}""")]
    [InlineData("""{"name":"   ","totalSeats":10}""")]
    [InlineData("""{"totalSeats":10}""")]
    [InlineData("""{"name":"Class 6","totalSeats":0}""")]
    [InlineData("""{"name":"Class 6","totalSeats":-5}""")]
    [InlineData("""{"name":"Class 6"}""")]
    [InlineData("""{"name":"Class 6","totalSeats":"many"}""")]
    [InlineData("{ this is not json")]
    public async Task Invalid_class_request_is_rejected_with_400(string json)
    {
        var response = await Client.PostAsync("/classes",
            new StringContent(json, Encoding.UTF8, "application/json"));

        await response.AssertErrorAsync(HttpStatusCode.BadRequest, "VALIDATION_ERROR");
    }

    [Fact]
    public async Task Class_name_longer_than_200_characters_is_rejected()
    {
        var json = $$"""{"name":"{{new string('x', 201)}}","totalSeats":10}""";

        var response = await Client.PostAsync("/classes",
            new StringContent(json, Encoding.UTF8, "application/json"));

        await response.AssertErrorAsync(HttpStatusCode.BadRequest, "VALIDATION_ERROR");
    }
}