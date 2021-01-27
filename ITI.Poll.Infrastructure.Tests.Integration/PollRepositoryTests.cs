using FluentAssertions;
using ITI.Poll.Tests;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace ITI.Poll.Infrastructure.Tests.Integration
{
    [TestFixture]
    public class PollRepositoryTests
    {
        [Test]
        public async Task create_poll()
        {
            using(PollContext pollContext = TestHelpers.CreatePollContext())
            {
                var pollContextAccessor = new PollContextAccessor(pollContext);
                var userRepository = new UserRepository(pollContextAccessor);
                var sut = new PollRepository(pollContextAccessor);
                var userService = TestHelpers.UserService;

                // create the user that'll later be the poll author
                var email = $"{Guid.NewGuid()}@test.org";
                var nickname = $"Test-{Guid.NewGuid()}";
                await userService.CreateUser(
                    userRepository,
                    email,
                    nickname,
                    "test-hash"
                );
                var author = await userService.FindByNickname(userRepository, nickname);

                // create the guests that'll be used to create the poll
                var guest1 = await TestHelpers.UserService.CreateUser(
                    userRepository,
                    $"{Guid.NewGuid()}@test.org",
                    $"Test-{Guid.NewGuid()}",
                    "test-hash"
                );
                var guest2 = await TestHelpers.UserService.CreateUser(
                    userRepository,
                    $"{Guid.NewGuid()}@test.org",
                    $"Test-{Guid.NewGuid()}",
                    "test-hash"
                );

                var poll = new Model.Poll(0, author.Value.UserId, "question?", false);
                poll.AddGuest(guest1.Value.UserId, await sut.GetNoProposal());
                poll.AddGuest(guest2.Value.UserId, await sut.GetNoProposal());
                poll.AddProposal("P1");
                poll.AddProposal("P2");

                var result = await sut.Create(poll);
                result.IsSuccess.Should().BeTrue();
                poll.PollId.Should().NotBe(0);

                var createdPoll = await sut.FindById(poll.PollId);
                createdPoll.Value.PollId.Should().Be(poll.PollId);
                
                await TestHelpers.UserService.DeleteUser(pollContext, userRepository, sut, author.Value.UserId);
                await TestHelpers.UserService.DeleteUser(pollContext, userRepository, sut, guest1.Value.UserId);
                await TestHelpers.UserService.DeleteUser(pollContext, userRepository, sut, guest2.Value.UserId);
            }
        }

        [Test]
        public async Task create_poll_with_invalid_authorId_should_return_an_error()
        {
            using(PollContext pollContext = TestHelpers.CreatePollContext())
            {
                var pollContextAccessor = new PollContextAccessor(pollContext);
                var userRepository = new UserRepository(pollContextAccessor);
                var sut = new PollRepository(pollContextAccessor);
                var userService = TestHelpers.UserService;

                var poll = new Model.Poll(0, -424, "question?", false);
                var result = await sut.Create(poll);
                result.IsSuccess.Should().BeFalse();
            }
        }
    }
}