﻿using PanCardView.Enums;
using PanCardView.Utility;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Forms;
using static PanCardView.Processors.Constants;
using static System.Math;

namespace PanCardView.Processors
{
    public class BaseCarouselBackViewProcessor : ICardBackViewProcessor
    {
        public uint AnimationLength { get; set; } = 300;

        public Easing AnimEasing { get; set; } = Easing.SinInOut;

        public double ScaleFactor { get; set; } = 1;

        public double OpacityFactor { get; set; } = 1;

        public double RotationFactor { get; set; } = 0;

        public virtual void HandleInitView(IEnumerable<View> views, CardsView cardsView, AnimationDirection animationDirection)
        {
            var view = views.FirstOrDefault();
            SetTranslationX(view, Sign((int)animationDirection) * cardsView.Width, cardsView, false);
        }

        public virtual void HandleCleanView(IEnumerable<View> views, CardsView cardsView)
        {
            var view = views.FirstOrDefault();
            SetTranslationX(view, cardsView.Width, cardsView, false);
        }

        public virtual void HandlePanChanged(IEnumerable<View> views, CardsView cardsView, double xPos, AnimationDirection animationDirection, IEnumerable<View> inactiveViews)
        {
            var view = views.FirstOrDefault();
            var inactiveView = inactiveViews.FirstOrDefault();

            if (view != null)
            {
                view.IsVisible = true;
            }
            if (inactiveView != null)
            {
                inactiveView.IsVisible = false;
            }

            if (animationDirection == AnimationDirection.Null)
            {
                return;
            }

            var value = Sign((int)animationDirection) * cardsView.Width + xPos;
            if (Abs(value) > cardsView.Width || (animationDirection == AnimationDirection.Prev && value > 0) || (animationDirection == AnimationDirection.Next && value < 0))
            {
                return;
            }
            SetTranslationX(view, value, cardsView);
        }

        public virtual Task HandleAutoNavigate(IEnumerable<View> views, CardsView cardsView, AnimationDirection animationDirection, IEnumerable<View> inactiveViews)
        {
            var view = views.FirstOrDefault();
            if (view == null)
            {
                return Task.FromResult(false);
            }

            var destinationPos = animationDirection == AnimationDirection.Prev
               ? cardsView.Width
               : -cardsView.Width;

            return new AnimationWrapper(v => SetTranslationX(view, v, cardsView), 0, destinationPos)
                .Commit(view, nameof(HandleAutoNavigate), 16, AnimationLength, AnimEasing);
        }

        public virtual Task HandlePanReset(IEnumerable<View> views, CardsView cardsView, AnimationDirection animationDirection, IEnumerable<View> inactiveViews)
        {
            var view = views.FirstOrDefault();
            if (view == null || view == cardsView.CurrentView)
            {
                return Task.FromResult(true);
            }
            var animTimePercent = (cardsView.Width - Abs(GetTranslationX(view))) / cardsView.Width;
            var animLength = (uint)(AnimationLength * animTimePercent) * 3 / 2;
            if (animLength == 0)
            {
                return Task.FromResult(true);
            }
            return new AnimationWrapper(v => SetTranslationX(view, v, cardsView), GetTranslationX(view), Sign((int)animationDirection) * cardsView.Width)
                .Commit(view, nameof(HandlePanReset), 16, animLength, AnimEasing);
        }

        public virtual Task HandlePanApply(IEnumerable<View> views, CardsView cardsView, AnimationDirection animationDirection, IEnumerable<View> inactiveViews)
        {
            var view = views.FirstOrDefault();
            if (view == null)
            {
                return Task.FromResult(true);
            }
            var animTimePercent = (cardsView.Width - Abs(GetTranslationX(view))) / cardsView.Width;
            var animLength = (uint)(AnimationLength * animTimePercent);
            if (animLength == 0)
            {
                return Task.FromResult(true);
            }
            return new AnimationWrapper(v => SetTranslationX(view, v, cardsView), GetTranslationX(view), -Sign((int)animationDirection) * cardsView.Width)
                .Commit(view, nameof(HandlePanReset), 16, animLength, AnimEasing);
        }

        protected virtual double GetTranslationX(View view)
        {
            if (view == null)
            {
                return 0;
            }
            var value = view.TranslationX;
            value += Sign(value) * view.Width * 0.5 * (1 - view.Scale);
            return value;
        }

        protected virtual void SetTranslationX(View view, double value, CardsView cardsView, bool? isVisible = null)
        {
            if(view == null)
            {
                return;
            }

            if(view.Width < 0)
            {
                void OnViewSizeChanged(object sender, System.EventArgs e)
                {
                    if(view.Width < 0)
                    {
                        return;
                    }
                    view.SizeChanged -= OnViewSizeChanged;
                    SetTranslationX(view, value, cardsView, isVisible);
                }
                view.SizeChanged += OnViewSizeChanged;
                return;
            }

            try
            {
                view.BatchBegin();
                view.Scale = CalculateFactoredProperty(value, ScaleFactor, cardsView);
                view.Opacity = CalculateFactoredProperty(value, OpacityFactor, cardsView);
                view.Rotation = CalculateFactoredProperty(value, RotationFactor, cardsView, 0) * Angle360 * Sign(-value);
                view.TranslationX = value - Sign(value) * view.Width * 0.5 * (1 - view.Scale);
                view.IsVisible = isVisible ?? view.IsVisible;
            }
            finally
            {
                view.BatchCommit();
            }
        }

        protected virtual double CalculateFactoredProperty(double value, double factor, CardsView cardsView, int defaultFactorValue = 1)
            => Abs(value) * (factor - defaultFactorValue) / cardsView.Width + defaultFactorValue;
    }
}