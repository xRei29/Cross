using Core.Domain;

namespace Core.Abstractions;

public interface IOrderStore
{
    IReadOnlyList<Order> List(); // повертає всі замовлення
    Order? GetById(string id); // повертає замовлення за його id
    void Add(Order item); // додає нове замовлення
    void Update(Order item); // оновлює існуюче замовлення
    bool Remove(string id); // видаляє замовлення за його id
    IReadOnlyList<Order> Find(Func<Order, bool> predicate); // повертає всі замовлення, які відповідають заданому предикату
}