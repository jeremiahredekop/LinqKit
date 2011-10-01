using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using NUnit.Framework;

namespace LinqKit.Tests
{
    [TestFixture]
    public class ExpandStaticMembersTests
    {
        public class Order
        {
            public List<OrderDetail> OrderDetails = new List<OrderDetail>();
            public decimal Total;
        }

        public class OrderDetail
        {
            public int Qty;
            public decimal Price;
        }

        public static class Calcs
        {
            // Static Fields
            public static Expression<Func<OrderDetail, decimal>> LineTotal = o => o.Qty * o.Price;
            public static Expression<Func<List<OrderDetail>, decimal>> OrderTotal = o => o.Sum(od => LineTotal.Invoke(od));

            // Static Properties
            static Calcs()
            {
                LineTotal2 = o => o.Qty * o.Price;
                OrderTotal2 = o => o.Sum(od => LineTotal2.Invoke(od));
            }
            public static Expression<Func<OrderDetail, decimal>> LineTotal2 { get; set; }
            public static Expression<Func<List<OrderDetail>, decimal>> OrderTotal2 { get; set; }

            // Static Methods
            public static Expression<Func<OrderDetail, decimal>> LineTotalMethod()
            {
                return o => o.Qty * o.Price;
            }

            public static Expression<Func<List<OrderDetail>, decimal>> OrderTotalMethod()
            {
                return o => o.Sum(od => LineTotalMethod().Invoke(od));
            }
        }
        [Test]
        public void Should_expand_static_fields()
        {

            var order = new Order();
            order.OrderDetails.Add(new OrderDetail { Price = .5M, Qty = 1 });
            order.OrderDetails.Add(new OrderDetail { Price = 4.25M, Qty = 2 });

            var expr = Calcs.OrderTotal.Expand();
            Debug.WriteLine(expr.Body);
            var output = expr.Compile().Invoke(order.OrderDetails);

            Assert.AreEqual(9M, output);
        }

        [Test]
        public void Should_expand_static_properties()
        {

            var order = new Order();
            order.OrderDetails.Add(new OrderDetail { Price = .5M, Qty = 1 });
            order.OrderDetails.Add(new OrderDetail { Price = 4.25M, Qty = 2 });

            var expr = Calcs.OrderTotal2.Expand();
            Debug.WriteLine(expr.Body);
            var output = expr.Compile().Invoke(order.OrderDetails);

            Assert.AreEqual(9M, output);
        }

        [Test]
        public void Should_expand_static_methods()
        {

            var order = new Order();
            order.OrderDetails.Add(new OrderDetail { Price = .5M, Qty = 1 });
            order.OrderDetails.Add(new OrderDetail { Price = 4.25M, Qty = 2 });

            var expr = Calcs.OrderTotalMethod().Expand();
            Debug.WriteLine(expr.Body);
            var output = expr.Compile().Invoke(order.OrderDetails);

            Assert.AreEqual(9M, output);
        }
    }
}
