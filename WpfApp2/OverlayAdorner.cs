using System;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace AnswerSearcher
{
    public class OverlayAdorner<TVisual> : Adorner, IDisposable where TVisual : UIElement, new()
    {
        private UIElement adorningElement;
        private AdornerLayer layer;

        public static IDisposable Overlay(UIElement elementToAdorn, TVisual adorningElement)
        {
            var adorner = new OverlayAdorner<TVisual>(elementToAdorn, adorningElement);
            adorner.layer = AdornerLayer.GetAdornerLayer(elementToAdorn);
            adorner.layer.Add(adorner);
            return adorner as IDisposable;
        }

        internal OverlayAdorner(UIElement adornedElement, UIElement adorningElement) : base(adornedElement)
        {
            this.adorningElement = adorningElement;
            if (adorningElement != null)
            {
                AddVisualChild(adorningElement);
            }
            Focusable = true;
        }

        protected override int VisualChildrenCount
        {
            get
            {
                return adorningElement == null ? 0 : 1;
            }
        }

        protected override Size MeasureOverride(Size constraint)
        {
            Size childConstraint = new Size(Double.PositiveInfinity, Double.PositiveInfinity);

            if (adorningElement != null)
            {
                adorningElement.Measure(childConstraint);
            }
            return new Size();
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            if (adorningElement != null)
            {
                adorningElement.Arrange(new Rect(new Point(0, 0), this.AdornedElement.RenderSize));
            }
            return finalSize;
        }

        protected override Visual GetVisualChild(int index)
        {
            if (index == 0 && adorningElement != null)
            {
                return adorningElement;
            }
            return base.GetVisualChild(index);
        }

        public void Dispose()
        {
            layer.Remove(this);
        }
    }
}
