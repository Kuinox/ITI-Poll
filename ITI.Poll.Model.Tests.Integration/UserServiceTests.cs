using System;
using System.Linq;
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

		[Test]
		public async Task heavy_load_test() // Nicolas
		{
			using (PollContext pollContext = TestHelpers.CreatePollContext())
			{
				PollContextAccessor pollContextAccessor = new PollContextAccessor(pollContext);
				UserRepository userRepository = new UserRepository(pollContextAccessor);

				int load = 2_000; // I started with 1M then 20k but it was too slow...
				Task<Result<User>>[] tasks = new Task<Result<User>>[load];
				for (int i = 0; i < load; i++)
				{
					tasks[i] = TestHelpers.UserService.CreateUser(userRepository, $"test-{Guid.NewGuid()}@test.fr", $"Test-{Guid.NewGuid()}", "validpassword");
				}
				await Task.WhenAll(tasks);
				tasks.Select(s => s.Result.IsSuccess).Should().NotContain(false);
			}
		}

		[TestCase("foo")]
		[TestCase("foo@bar")]
		[TestCase("plainaddress")]
		[TestCase("#@%^%#$@#$@#.com")]
		[TestCase("@example.com")]
		[TestCase("Joe Smith <email @example.com>")]
		[TestCase("email.example.com")]
		[TestCase("email@example @example.com")]
		[TestCase(".email @example.com")]
		[TestCase("email.@example.com")]
		[TestCase("email..email @example.com")]
		[TestCase("email@example.com (Joe Smith)")]
		[TestCase("email@example")]
		[TestCase("email@-example.com")]
		[TestCase("email@example.web")]
		[TestCase("email@111.222.333.44444")]
		[TestCase("email @example..com")]
		[TestCase("Abc..123@example.com")]
		[TestCase("”(),:;<>[\\]")]
		[TestCase("@example.com")]
		[TestCase("just”not”right @example.com")]
		[TestCase("this\\ is\"really\"not\\allowed @example.com")]
		public async Task cant_create_account_with_invalid_email(string email)
		{
			using (PollContext pollContext = TestHelpers.CreatePollContext())
			{
				PollContextAccessor pollContextAccessor = new PollContextAccessor(pollContext);
				UserRepository userRepository = new UserRepository(pollContextAccessor);
				string nickname = $"Test-{Guid.NewGuid()}";

				Result<User> user = await TestHelpers.UserService.CreateUser(userRepository, email, nickname, "validpassword");

				user.IsSuccess.Should().BeFalse();
			}
		}


		[TestCase(@"email @example.com")]
		[TestCase(@"firstname.lastname @example.com")]
		[TestCase(@"email@subdomain.example.com")]
		[TestCase(@"firstname +lastname @example.com")]
		[TestCase(@"email@123.123.123.123")]
		[TestCase(@"email@[123.123.123.123]")]
		[TestCase(@"""email""@example.com")]
		[TestCase(@"1234567890@example.com")]
		[TestCase(@"email@example-one.com")]
		[TestCase(@"_______@example.com")]
		[TestCase(@"email@example.name")]
		[TestCase(@"email@example.museum")]
		[TestCase(@"email@example.co.jp")]
		[TestCase(@"firstname-lastname @example.com")]
		[TestCase(@"much.”more\ unusual”@example.com")]
		[TestCase(@"very.unusual.”@”.unusual.com @example.com")]
		[TestCase(@"very.”(),:;<>[]”.VERY.”very@\\ ""very”.unusual@strange.example.com")]
		public async Task can_create_account_with_weird_valid_email(string email)
		{
			using (PollContext pollContext = TestHelpers.CreatePollContext())
			{
				PollContextAccessor pollContextAccessor = new PollContextAccessor(pollContext);
				UserRepository userRepository = new UserRepository(pollContextAccessor);
				string nickname = $"Test-{Guid.NewGuid()}";

				Result<User> user = await TestHelpers.UserService.CreateUser(userRepository, email, nickname, "validpassword");

				user.IsSuccess.Should().BeTrue();
			}
		}
	}
}
