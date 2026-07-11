using PlayGround.Shared.Result;
using Xunit;

namespace PlayGround.Tests.Unit.Core
{
    public class ResultTests
    {
        [Fact]
        public void Success_ReturnsValueAndSuccessState()
        {
            var result = Result<int>.Success(42);

            Assert.True(result.IsSuccess);
            Assert.False(result.IsFailure);
            Assert.Equal(42, result.Value);
        }

        [Fact]
        public void Error_ReturnsFailureState()
        {
            var result = Result<int>.Error(ErrorCode.NotFound);

            Assert.False(result.IsSuccess);
            Assert.True(result.IsError);
            Assert.Equal(ErrorCode.NotFound, result.ResultData.DetailCode);
        }

        [Fact]
        public void FromException_ArgumentNullException_MapsToMissingRequired()
        {
            var result = Result<int>.FromException(new ArgumentNullException("param"));

            Assert.True(result.IsError);
            Assert.Equal(ErrorCode.MissingRequired, result.ResultData.DetailCode);
        }

        [Fact]
        public void FromException_ArgumentException_MapsToInvalidInput()
        {
            var result = Result<int>.FromException(new ArgumentException("bad"));

            Assert.True(result.IsError);
            Assert.Equal(ErrorCode.InvalidInput, result.ResultData.DetailCode);
        }

        [Fact]
        public void Map_OnSuccess_TransformsValue()
        {
            var result = Result<int>.Success(10).Map(x => x * 2);

            Assert.True(result.IsSuccess);
            Assert.Equal(20, result.Value);
        }

        [Fact]
        public void Bind_OnError_PropagatesFailure()
        {
            var result = Result<int>.Error(ErrorCode.NotFound).Bind(x => Result<string>.Success(x.ToString()));

            Assert.True(result.IsError);
            Assert.Equal(ErrorCode.NotFound, result.ResultData.DetailCode);
        }

        [Fact]
        public void CombineAll_EmptyInput_ReturnsSuccessWithEmptyArray()
        {
            var result = ResultExtensions.CombineAll<int>();

            Assert.True(result.IsSuccess);
            Assert.Empty(result.Value!);
        }

        [Fact]
        public void CombineAny_EmptyInput_ReturnsUnknown()
        {
            var result = ResultExtensions.CombineAny<int>();

            Assert.True(result.IsError);
        }

        [Fact]
        public void CombineAny_ReturnsFirstSuccess()
        {
            var result = ResultExtensions.CombineAny(
                Result<int>.Error(ErrorCode.NotFound),
                Result<int>.Success(7));

            Assert.True(result.IsSuccess);
            Assert.Equal(7, result.Value);
        }

        [Fact]
        public void ToEnvelope_Success_MapsCodeAndData()
        {
            var envelope = Result<string>.Success("data").ToEnvelope();

            Assert.True(envelope.IsSuccess);
            Assert.Equal("data", envelope.Data);
            Assert.Equal(SuccessCode.Ok.Value, envelope.Code);
        }

        [Fact]
        public void ToHttpResponse_NotFound_Maps404()
        {
            var response = Result<string>.Error(ErrorCode.NotFound).ToHttpResponse();

            Assert.Equal(404, response.StatusCode);
            Assert.False(response.IsSuccess);
        }
    }
}
