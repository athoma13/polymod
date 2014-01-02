using Polymod.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Polymod.Fluent
{

    public class ProxyBuilderFluentNode
    {
    }

    public static class ProxyBuilderHelper
    {
        public static void AddAspectBuilder(this ProxyBuilder pb, IAspectBuilder builder)
        {
            pb.AddBuilder(builder);
        }
        public static TFluentNode AddAspectBuilder<TFluentNode>(this ProxyBuilder pb, IFluentAspectBuilder<TFluentNode> builder)
        {
            pb.AddBuilder(builder.Builder);
            return builder.CreateFluentNode(pb);
        }

    }







    public static class FluentModelBuilder
    {
    }

    public class FluentModelBuilder<TSource>
    {
        public ProxyBuilder ModelBuilder { get; private set; }
        public FluentModelBuilder()
        {
            ModelBuilder = new ProxyBuilder();
        }
    }

    public interface IFluentNode
    {
        string ToString();

    }
    public class FluentNodeBase  : IFluentNode
    {

    }



    public class FluentNotificationAspectBuilder<TSource> 
    {
        NotificationAspectBuilder<TSource> _aspectBuilder;

        public FluentNotificationAspectBuilder(NotificationAspectBuilder<TSource> aspectBuilder)
        {
            _aspectBuilder = aspectBuilder;
        }

        public FluentNotificationAspectBuilder<TSource> AddChange<TParameter1, TParameter2>(Expression<Func<TSource, TParameter1>> sourceExpression, Expression<Func<TSource, TParameter2>> affectedExpression)
        {
            _aspectBuilder.AddNotification(ExpressionHelper.GetPropertyName(sourceExpression), ExpressionHelper.GetPropertyName(affectedExpression));
            return this;
        }
    }


    public static class FluentModelBuilderExtensions
    {
        public static FluentNotificationAspectBuilder<TSource> AddNotificationAspect<TSource>(this FluentModelBuilder<TSource> fluentModelBuilder)
        {
            //fluentModelBuilder.ModelBuilder.Find(ab => ab
            //TODO: Reuse the current NotificationAspectBuilder if one already exists.
            var aspectBuilder = new NotificationAspectBuilder<TSource>();
            return new FluentNotificationAspectBuilder<TSource>(aspectBuilder);
        }
    }
}
