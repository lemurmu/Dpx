using SciChart.Charting.Model.DataSeries;
using SciChart.Charting.Model.DataSeries.Heatmap2DArrayDataSeries;
using SciChart.Charting.Visuals.PaletteProviders;
using SciChart.Charting.Visuals.RenderableSeries;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Dpx
{
    public class CustomUniformHeatmapRenderableSeries : FastUniformHeatmapRenderableSeries
    {
        protected override string FormatDataValue(double dataValue, int xIndex, int yIndex)
        {
            IPointMetadata[,] metaDatas = ((IHeatmapDataSeries)DataSeries).Metadata;
            if (metaDatas != null)
            {
                var metaData = (UniformHeatmapMetaData)metaDatas[yIndex, xIndex];
                return metaData.IsBody ? base.FormatDataValue(dataValue, xIndex, yIndex) : string.Empty;
            }
            return base.FormatDataValue(dataValue, xIndex, yIndex); ;
        }
    }

    public class UniformHeatmapMetaData : IPointMetadata
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public bool IsSelected { get; set; }

        public Color CellColor { get; set; }
        public bool IsBody { get; set; }
        public string Tooltip { get; set; }
    }

    public class HeatmapMetaDataPaletteProvider : IHeatmapPaletteProvider
    {
        public void OnBeginSeriesDraw(IRenderableSeries rSeries)
        {
        }

        public Color? OverrideCellColor(IRenderableSeries rSeries, int xIndex, int yIndex, IComparable zValue, Color cellColor, IPointMetadata metadata)
        {
            if (metadata != null)
            {
                cellColor = ((UniformHeatmapMetaData)metadata).CellColor;
            }

            return cellColor;
        }
    }
}
