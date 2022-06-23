using Microsoft.EntityFrameworkCore;
using MiniShopApp.Data.Abstract;
using MiniShopApp.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniShopApp.Data.Concrete.EfCore
{
    public class EfCoreCardRepository : EfCoreGenericRepository<Card>, ICardRepository
    {
        public EfCoreCardRepository(MiniShopContext context) : base(context)
        {

        }
        private MiniShopContext MiniShopContext
        {
            get { return _context as MiniShopContext; }
        }
        public void ClearCard(int cardId)
        {
            var cmd = @"DELETE FROM CardItems WHERE CardId=@p0"; //sql srogusunu kendımız yazmamı gerekir
            MiniShopContext.Database.ExecuteSqlRaw(cmd, cardId);// sql serverde exequte et bu ıslemlerı bu parametre bılgılerıne gore
        }

        public void DeleteFromCard(int cardId, int productId)
        {
            var cmd = @"DELETE FROM CardItems WHERE CardId=@p0 AND ProductId=@p1"; //sql srogusunu kendımız yazmamı gerekir
            MiniShopContext.Database.ExecuteSqlRaw(cmd, cardId, productId);// sql serverde exequte et bu ıslemlerı bu parametre bılgılerıne gore
        }

        public Card GetByUserId(string userId)
        {
            return MiniShopContext.Cards
                .Include(i => i.CardItems)
                .ThenInclude(i => i.Product)
                .FirstOrDefault(i => i.UserId == userId);
        }
        public override void Update(Card entity)
        {
            MiniShopContext.Cards.Update(entity);
        }
    }
}
