using ads.Data;
using ads.Interface;
using ads.Models.Data;
using Microsoft.EntityFrameworkCore;

namespace ads.Repository
{
    public class ClubRepo : IClub
    {
        private readonly AdsContext _context;

        public ClubRepo(AdsContext context)
        {
            _context = context;
        }

        public async Task<List<Club>> GetAllClubs()
        {
            var clubs = await _context.Clubs.ToListAsync();

            return clubs;
        }

        public async Task<Dictionary<int, DateTime>> GetClubsDictionary()
        {
            var clubs = await _context.Clubs.ToListAsync();
            var dictionary = clubs.DistinctBy(x => x.Number).ToDictionary(x => x.Number, y => y.StartDate);

            return dictionary;
        }
    }
}
