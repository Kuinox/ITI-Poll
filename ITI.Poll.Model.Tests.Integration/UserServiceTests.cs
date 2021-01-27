using System;
using System.Threading.Tasks;
using FluentAssertions;
using ITI.Poll.Infrastructure;
using ITI.Poll.Tests;
using NUnit.Framework;

namespace ITI.Poll.Model.Tests.Integration
{
	public class UserServiceTests
	{
		[Test]
		public async Task create_user()
		{
			using (PollContext pollContext = TestHelpers.CreatePollContext())
			{
				PollContextAccessor pollContextAccessor = new PollContextAccessor(pollContext);
				UserRepository userRepository = new UserRepository(pollContextAccessor);
				string email = $"test-{Guid.NewGuid()}@test.fr";
				string nickname = $"Test-{Guid.NewGuid()}";

				Result<User> user = await TestHelpers.UserService.CreateUser(userRepository, email, nickname, "validpassword");

				user.IsSuccess.Should().BeTrue();
				user.Value.Email.Should().Be(email);
				user.Value.Nickname.Should().Be(nickname);

				user = await TestHelpers.UserService.FindByNickname(userRepository, nickname);
				user.IsSuccess.Should().BeTrue();

				PollRepository pollRepository = new PollRepository(pollContextAccessor);
				await TestHelpers.UserService.DeleteUser(pollContext, userRepository, pollRepository, user.Value.UserId);
			}
		}

		[Test]
		public async Task deleted_user_cannot_create_poll() // Nicolas
		{
			using (PollContext pollContext = TestHelpers.CreatePollContext())
			{
				PollContextAccessor pollContextAccessor = new PollContextAccessor(pollContext);
				UserRepository userRepository = new UserRepository(pollContextAccessor);
				PollRepository pollRepository = new PollRepository(pollContextAccessor);

				Result<User> user = await TestHelpers.UserService.CreateUser(userRepository, $"test-{Guid.NewGuid()}@test.fr", $"Test-{Guid.NewGuid()}", "validpassword");
				Result<User> userGuest = await TestHelpers.UserService.CreateUser(userRepository, $"test-{Guid.NewGuid()}@test.fr", $"Test-{Guid.NewGuid()}", "validpassword");
				Result res = await TestHelpers.UserService.DeleteUser(pollContext, userRepository, pollRepository, user.Value.UserId);
				res.IsSuccess.Should().BeTrue();

				Result<Poll> pollRes = await TestHelpers.PollService.CreatePoll(pollContext, pollRepository, userRepository, new NewPollDto()
				{
					AuthorId = user.Value.UserId,
					Question = "A question",
					Proposals = new string[] { "Proposal A", "Proposal B" },
					GuestNicknames = new string[] { userGuest.Value.Nickname }
				});
				pollRes.Error.Should().Be(Errors.AccountDeleted);
			}
		}
	}
}
