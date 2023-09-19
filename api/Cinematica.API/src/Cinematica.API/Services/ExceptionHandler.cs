using Amazon.CognitoIdentityProvider.Model;
using System;

namespace Cinematica.API.Services
{
    public class ExceptionHandler
    {
        public static string HandleException(Exception ex)
        {
            string message;

            switch (ex)
            {
                case UserNotFoundException unfEx:
                    message = "A Username not found exception has occurred: " + unfEx.Message;
                    break;
                case UserNotConfirmedException uncEx:
                    message = "A user not confirmed exception has occurred: " + uncEx.Message;
                    break;
                case NotAuthorizedException naEx:
                    message = "A not authorized exception has occurred: " + naEx.Message;
                    break;
                case InvalidParameterException ipEx:
                    message = "An invalid parameter exception has occurred: " + ipEx.Message;
                    break;
                case CodeMismatchException cmmEx:
                    message = "A code mismatch exception has occurred: " + cmmEx.Message;
                    break;
                case ExpiredCodeException ecEx:
                    message = "A expired code exception has occurred: " + ecEx.Message;
                    break;
                case InvalidPasswordException ipwEx:
                    message = "An invalid password exception has occurred: " + ipwEx.Message;
                    break;
                case UsernameExistsException uneEx:
                    message = "A username exists exception has occurred: " + uneEx.Message;
                    break;
                default:
                    message = "An " + ex.GetType().ToString() + "has occurred: " + ex.Message;
                    break;
            }

            // Log the error message
            Console.WriteLine(message);

            // Return the type of the exception
            return ex.GetType().ToString();
        }
    }
}
