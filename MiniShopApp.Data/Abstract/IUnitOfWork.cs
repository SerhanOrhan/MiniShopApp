using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniShopApp.Data.Abstract
{
    public interface IUnitOfWork: IDisposable  //burda daha genel bi veritabanı kayıt interfaceden turetırız.
    {
        //Repositorylerimizi gruplayacağız
        ICardRepository Cards { get; }
        ICategoryRepository Categories { get; }
        IOrderRepository Orders { get; }
        IProductRepository Products { get; }
        void Save();
        Task<int> SaveAsync();
    }
}
