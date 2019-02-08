﻿using System;
using System.Collections.Generic;
using System.Text;
using TensorSharp;

namespace SiaNet.Layers
{
    public class MaxPooling2D : BaseLayer
    {
        public Tuple<uint, uint> PoolSize { get; set; }

        public uint Strides { get; set; }

        public PaddingType Padding { get; set; }

        private Tensor xCols;

        public MaxPooling2D(Tuple<uint, uint> poolSize = null, uint strides = 1, PaddingType padding = PaddingType.Same)
            : base("maxpooling2d")
        {
            PoolSize = poolSize ?? Tuple.Create<uint, uint>(2, 2);
            Strides = strides;
            Padding = padding;
        }

        public override void Forward(Tensor x)
        {
            Input = x.ToParameter();
            var (n, c, h, w) = x.GetConv2DShape();

            int pad = 0;
            if (Padding == PaddingType.Same)
            {
                pad = 1;
            }
            else if (Padding == PaddingType.Full)
            {
                pad = 2;
            }

            var h_out = (h - PoolSize.Item1 + 2 * pad) / Strides + 1;
            var w_out = (w - PoolSize.Item2 + 2 * pad) / Strides + 1;

            var x_reshaped = x.Reshape(n * c, 1, h, w);
            xCols = ImgUtil.Im2Col(x_reshaped, PoolSize, pad, Strides);
            Output = Max(xCols, 0);
            Output = Output.Reshape(h_out, w_out, n, c).Transpose(2, 3, 0, 1);
        }

        public override void Backward(Tensor outputgrad)
        {
            var dX_col = Tensor.Constant(0, Global.Device, DType.Float32, xCols.Shape);
            var (n, c, h, w) = Input.Data.GetConv2DShape();
            int pad = 0;
            if (Padding == PaddingType.Same)
            {
                pad = 1;
            }
            else if (Padding == PaddingType.Full)
            {
                pad = 2;
            }

            var dout_flat = outputgrad.Transpose(2, 3, 0, 1);
            var dX = ImgUtil.Col2Im(dout_flat, Input.Data.Shape, PoolSize, pad, Strides);
            Input.Grad = dX.Reshape(n, c, h, w);
        }
    }
}
