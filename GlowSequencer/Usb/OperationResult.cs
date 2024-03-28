#nullable enable

namespace GlowSequencer.Usb
{
    public class OperationResult
    {
        public bool IsSuccess { get; protected set; }
        public string ErrorMessage { get; protected set; }

        protected OperationResult(bool isSuccess, string errorMessage)
        {
            IsSuccess = isSuccess;
            ErrorMessage = errorMessage;
        }

        public static OperationResult Fail(string message) => new OperationResult(false, message);

        public static OperationResult Success() => new OperationResult(true, string.Empty);

        public static OperationResult Fail(string message, string innerMessage)
        {
            return new OperationResult(false, message + "\n\t" + innerMessage);
        }


        public bool IsFail(out OperationResult operationResult)
        {
            operationResult = this;
            return !IsSuccess;
        }
        
        public override string ToString()
        {
            return IsSuccess ? "Success" : ErrorMessage;
        }
    }

    public class OperationResult<T> : OperationResult
    {
        public T? Data { get; private set; }

        private OperationResult(bool isSuccess, string errorMessage, T data) : base(isSuccess, errorMessage)
        {
            Data = data;
        }

        public static OperationResult<T> Success(T data) => new OperationResult<T>(true, string.Empty, data);

        public new static OperationResult<T?> Fail(string message) => new OperationResult<T?>(false, message, default);

        public new static OperationResult<T?> Fail(string message, string innerMessage)
        {
            return new OperationResult<T?>(false, message + "\n\t" + innerMessage, default);
        }

        public bool IsSuccessWithResult(out T data)
        {
            data = Data!;
            return IsSuccess;
        }

        public bool IsSuccessWithResultAndMessage(out T usbDevice, out string s)
        {
            usbDevice = Data!;
            s = ErrorMessage;
            return IsSuccess;
        }

        public bool IsFail(out OperationResult<T> operationResult)
        {
            operationResult = this;
            return !IsSuccess;
        }

        public bool IsFailWithNewOperatingResult<T2>(out OperationResult<T2> operationResult)
        {
            operationResult = OperationResult<T2>.Fail(ErrorMessage)!;
            return !IsSuccess;
        }

        public bool IsFailWithNewOperatingResultAndData(out OperationResult operationResult, out T data)
        {
            operationResult = OperationResult.Fail(ErrorMessage);
            data = Data!;
            return !IsSuccess;
        }
        public bool IsFailWithNewOperatingResultAndData<T2>(out OperationResult<T2> operationResult, out T data)
        {
            operationResult = OperationResult<T2>.Fail(ErrorMessage)!;
            data = Data!;
            return !IsSuccess;
        }

    }
}
