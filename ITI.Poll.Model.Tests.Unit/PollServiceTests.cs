using System;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace ITI.Poll.Model.Tests.Unit
{
    [TestFixture]
    public class PollServiceTests
    {
        [Test]
        public async Task create_poll() //Hugo
        {
            var sut = new PollService();
            var unitOfWork = Substitute.For<IUnitOfWork>();
            var pollRepository = Substitute.For<IPollRepository>();
            pollRepository.Create( Arg.Any<Poll>() ).Returns( Result.CreateSuccess );
            var userRepository = Substitute.For<IUserRepository>();
            userRepository.FindById( Arg.Any<int>() ).Returns( Result.CreateSuccess( new User( 1234, "test@test.fr", "Test", "hash", false ) ) );
            userRepository.FindByNickname( Arg.Any<string>() ).Returns( Result.CreateSuccess( new User( 1234, "test@test.fr", "Test", "hash", false ) ) );

            var poll = await sut.CreatePoll( unitOfWork, pollRepository, userRepository, new NewPollDto() { AuthorId = 1234, Question = "Question ?", GuestNicknames = new[] { "test1", "test2" }, Proposals = new[] { "P1", "P2" } } );

            poll.IsSuccess.Should().BeTrue();
        }

        [Test]
        public async Task delete_poll() //Hugo
        {
            var sut = new PollService();
            var unitOfWork = Substitute.For<IUnitOfWork>();
            var pollRepository = Substitute.For<IPollRepository>();
            pollRepository.FindById( Arg.Any<int>() ).Returns( Result.CreateSuccess( new Poll( 1234, 1245, "Question ?", false) ) );

            var poll = await sut.DeletePoll( unitOfWork, pollRepository, 1234 );

            poll.IsSuccess.Should().BeTrue();
        }

        [Test]
        public async Task delete_guest() //Hugo
        {
            var sut = new PollService();
            var pollRepository = Substitute.For<IPollRepository>();
            var poll = new Poll( 1234, 1245, "Question ?", false );
            poll.AddGuest( 1234, null );
            pollRepository.FindById( Arg.Any<int>() ).Returns( Result.CreateSuccess( poll ) );

            var guest = await sut.DeleteGuest(pollRepository, 1234, 1245 );

            guest.IsSuccess.Should().BeTrue();
        }
    }
}
