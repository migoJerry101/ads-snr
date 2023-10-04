﻿using ads.Data;
using ads.Models.Data;

namespace ads.Interface
{
    public interface IOpenQuery
    {
        Task<List<GeneralModel>> ListOfAllSKu(OledbCon db);
        Task<List<GeneralModel>> ListOfSales(OledbCon db, string start, string end);
        Task<List<GeneralModel>> ListIventory(OledbCon db);
        Task<List<GeneralModel>> ListOfAllStore(OledbCon db);
    }
}