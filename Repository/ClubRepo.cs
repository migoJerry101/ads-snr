using ads.Data;
using ads.Interface;
using ads.Models.Data;
using Microsoft.EntityFrameworkCore;

namespace ads.Repository
{
    public class ClubRepo : IClub
    {
        private readonly AdsContex _contex;

        public ClubRepo(AdsContex contex)
        {
            _contex = contex;
        }

        public async Task<List<Club>> GetAllClubs()
        {
            var clubs = await _contex.Clubs.ToListAsync();

            return clubs;
        }

        public async Task<Dictionary<int, DateTime>> GetClubsDictionary()
        {
            var clubs = await _contex.Clubs.ToListAsync();
            var dictionary = clubs.DistinctBy(x => x.Number).ToDictionary(x => x.Number, y => y.StartDate);

            return dictionary;
        }

        public async Task<Dictionary<int, int>> GetClubsDictionaryByDate(DateTime date)
        {
            try
            {
               var clubs = await _contex.Clubs.Where(x => x.StartDate < date).DistinctBy(y => y.Number).ToDictionaryAsync(y => y.Number, z => z.Number);
     
                return clubs;
            }
            catch (Exception)
            {

                throw;
            }
        }
    }
}
