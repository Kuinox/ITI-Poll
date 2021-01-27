using FluentAssertions;
using ITI.Poll.Infrastructure;
using ITI.Poll.Tests;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ITI.Poll.Model.Tests.Integration
{
	public class PollServiceTests
	{

		[Test]
		public async Task can_create_poll() // Nicolas
		{
			using (PollContext pollContext = TestHelpers.CreatePollContext())
			{
				PollContextAccessor pollContextAccessor = new PollContextAccessor(pollContext);
				UserRepository userRepository = new UserRepository(pollContextAccessor);
				PollRepository pollRepository = new PollRepository(pollContextAccessor);

				Result<User> user = await TestHelpers.UserService.CreateUser(userRepository, $"test-{Guid.NewGuid()}@test.fr", $"Test-{Guid.NewGuid()}", "validpassword");
				Result<User> userGuest = await TestHelpers.UserService.CreateUser(userRepository, $"test-{Guid.NewGuid()}@test.fr", $"Test-{Guid.NewGuid()}", "validpassword");

				Result<Poll> pollRes = await TestHelpers.PollService.CreatePoll(pollContext, pollRepository, userRepository, new NewPollDto()
				{
					AuthorId = user.Value.UserId,
					Question = "A question",
					Proposals = new string[] { "Proposal A", "Proposal B" },
					GuestNicknames = new string[] { userGuest.Value.Nickname }
				});
				pollRes.IsSuccess.Should().BeTrue();
			}
		}
	}
}
