
using LMFS.Models;

namespace LMFS.Messages
{
    public class LoginSuccessMessage(User user)
    {
        public User User { get; set; } = user;
    }
}
