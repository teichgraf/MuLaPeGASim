using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Threading;
using System.Reflection;

using MultilayerNet;
using OCRPreProcessing;

namespace MuLaPeGASim
{
	// TODO: NetDesigner durch Klick auf Neuron in Grafik, highlighten des entsprechenden Neurons & aufklappen des Trees 
	// DONE: in main methode exceptionhandling einschalten
	// DONE: Console Ausschriften unterdrücken -> danach Projekteigenschaften von Console- auf Windowsanwendung
	// DONE: automaticLearning löschen

	/// <summary>
	/// Description: GUI for the Preprocessing / OCR application
	/// Author: Rene Schulte, Torsten Bär
	/// Version: 0.5
	/// Last recent Update: 17.11.2004
	/// </summary>
	public class GUI : System.Windows.Forms.Form
	{
		#region Variablen/Events

		private System.Windows.Forms.Timer t;
		private IImageFilter[] imgFilters;
		private HistogramSeparator imgSeparator;
		private Thread calcThread;
		private int progressPercent;		
		private ArrayList xValues, fxValues;
		private float errorMax, sx=1, sy=1;		
		private string xAxisText, yAxisText;
		private Pen pen, arrowPen;
		private Font axisFont;
		private SolidBrush axisBrush;
		private NeuralNet nn;
		private LearningAlgo[] learningAlgos;
		private bool isBackpropBatchMethod;
		private errorHandler visualizeErrorProgress;
		// t-bä
		//		private LearningAlgo.handleException displayGeneration;
		private DateTime startTime;
		//r-s
		private Queue lastErrors;
		//r-s
		private const int QUEUE_SIZE = 5000;
		//r-s
		private bool threadStopFromLearnAutomation = false;
		//r-s
		private errorHandler checkLastErrorsAndCtrlLearning;
		//	private bool hasSysInfosBeenWritten = false;

		// t-bä
		private IActivationFunction[] actFuncs;
		private ProgressBar[] progBars;
		private Label[] labels;
		private String[,] extractFuncs;
		private bool useTimeAsSeed, useBestRange;
		private int randSeed, randSeedDefault = 47110815;
		private float rangeMin, rangeMax;
		// changed
		private enum ThreadStates { START, STOP };
		//	private Bitmap bmp;
		private string formTitle;
		private ArrayList charBmpsAL;
		private int charsCount;
		private Bitmap filteredBmp;
		private Bitmap srcBmp;
		private event OCRPreProcessing.progressHandler onOCRCharComputing;
		private bool isDirty;
		private float charPBSx=1, charPBSy=1;
		private float charRgnSx=1, charRgnSy=1;
		private const string OCR_PREPROC_ASMNAME = "OCRPreProcessing";
		private const string DEF_NET_PATH = "examples/net/default.net";
		//	private Bitmap bmp;

		//r-s-21.9.
		/// <summary>
		/// the mean steepness of all neurons with a sigmoid activation function
		/// </summary>	
		private float steepnessOfAllSigmoidNeuronsProp
		{
			get
			{
				float sum = 0.0f;
				int nNeurons = 0;
				for(int i=0; i<nn.layersProp.Length; i++)
				{	
					nNeurons += nn.layersProp[i].neuronsProp.Length;
					for(int j=0; j<nn.layersProp[i].neuronsProp.Length; j++)
						sum += (nn.layersProp[i].neuronsProp[j].actFuncProp as SigmoidActFunc).steepnessProp;
				}
				return sum / nNeurons;
			}
			set
			{
				for(int i=0; i<nn.layersProp.Length; i++)
					for(int j=0; j<nn.layersProp[i].neuronsProp.Length; j++)
						(nn.layersProp[i].neuronsProp[j].actFuncProp as SigmoidActFunc).steepnessProp = value;
			}
		}

		#region WindowForms Varis
		private System.Windows.Forms.PictureBox errorPB;
		private System.Windows.Forms.StatusBar statusBar;
		private System.Windows.Forms.StatusBarPanel progressSBP;
		private System.Windows.Forms.Label errorGraphLabel;
		private System.Windows.Forms.Label errorListViewLabel;
		private System.Windows.Forms.ListView errorListView;
		private System.Windows.Forms.ColumnHeader CycleHeader;
		private System.Windows.Forms.ColumnHeader ErrorHeader;
		private System.Windows.Forms.StatusBarPanel percentSBP;
		private System.Windows.Forms.TabPage trainPage;
		private System.Windows.Forms.MainMenu mainMenu;
		private System.Windows.Forms.MenuItem fileMenuItem;
		private System.Windows.Forms.MenuItem fileMenuLoadItem;
		private System.Windows.Forms.MenuItem netMenuItem;
		private System.Windows.Forms.MenuItem netMenuRandomizeItem;		
		private System.Windows.Forms.CheckBox fastModeCheckBox;
		private System.Windows.Forms.Label fastModeLabel;
		private System.Windows.Forms.TabControl mainTabCtrl;
		private System.Windows.Forms.TabPage designPage;
		private System.Windows.Forms.PictureBox srcImgPB;
		private System.Windows.Forms.Label srcImgLab;
		private System.Windows.Forms.MenuItem fileMenuNewItem;
		private System.Windows.Forms.Button startTrainBtn;
		private System.Windows.Forms.Button stopTrainBtn;
		private System.Windows.Forms.MenuItem fileMenuSaveItem;
		private System.Windows.Forms.OpenFileDialog openFileDialogNN;
		private System.Windows.Forms.SaveFileDialog saveFileDialogNN;
		private System.Windows.Forms.OpenFileDialog openFileDialogPic;
		private System.Windows.Forms.MenuItem fileMenuItemStrich1;
		private System.Windows.Forms.MenuItem fileMenuLoadPicItem;
		private System.Windows.Forms.TabPage patternPage;
		private System.Windows.Forms.GroupBox patternModeGroupBox;
		private System.Windows.Forms.Panel ocrModePanel;
		private System.Windows.Forms.Panel manualModePanel;
		private System.Windows.Forms.MenuItem infoMenuItem;
		private System.Windows.Forms.MenuItem infoMenuAboutItem;
		private System.Windows.Forms.ComboBox learnAlgoComboBox;
		private System.Windows.Forms.Label learnAlgoLabel;
		private ParamAdjuster.ParamAdjuster[] learnAlgoParamAdjuster;
		private System.Windows.Forms.NumericUpDown cyclesUpDown;
		private System.Windows.Forms.Label cyclesLabel;
		private System.Windows.Forms.ToolTip toolTip;
		private System.Windows.Forms.RadioButton batchLearnRadioButton;
		private System.Windows.Forms.MenuItem menuItem1;
		private System.Windows.Forms.MenuItem fileMenuExitItem;
		private System.Windows.Forms.StatusBarPanel infoSBP;
		private System.Windows.Forms.StatusBarPanel timeSBP;
		private System.Windows.Forms.Button showNetBtn;
		private System.Windows.Forms.Button clearBtn;
		private System.Windows.Forms.TextBox neuronsNumTxt;
		private System.Windows.Forms.Button generateNetBtn;
		private System.Windows.Forms.StatusBarPanel remainingSBP;
		private System.Windows.Forms.TreeView patternTreeView;
		private System.Windows.Forms.ListView patternOutputListView;
		private System.Windows.Forms.ColumnHeader OutputNeuron;
		private System.Windows.Forms.ColumnHeader OutputValue;
		private System.Windows.Forms.Button newPatternBtn;
		private System.Windows.Forms.Button clearPaternsBtn;
		private System.Windows.Forms.Button currentPatternBtn;
		private System.Windows.Forms.TextBox outputTxt;
		private System.Windows.Forms.TextBox inputTxt;
		private System.Windows.Forms.ListView patternInputListView;
		private System.Windows.Forms.ColumnHeader InputNeuron;
		private System.Windows.Forms.ColumnHeader InputValue;
		private System.Windows.Forms.Label currentErrorTxt;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Button changeOutputPatternBtn;
		private System.Windows.Forms.TreeView netTreeView;
		private System.Windows.Forms.Button deletePatternBtn;
		private System.Windows.Forms.Button deleteAllPaternsBtn;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.TextBox netNameTxt;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.Button patternTreeExpand_Btn;
		private System.Windows.Forms.Button patternTreeCollapse_Btn;
		private System.Windows.Forms.Button netTreeCollapse_Btn;
		private System.Windows.Forms.Button netTreeExpand_Btn;
		private System.Windows.Forms.ComboBox activationFuncComboBox;
		private System.Windows.Forms.Label label6;
		private System.Windows.Forms.MenuItem menuItem2;
		private System.Windows.Forms.OpenFileDialog openFileDialogPattern;
		private System.Windows.Forms.SaveFileDialog saveFileDialogPattern;
		private System.Windows.Forms.MenuItem fileMenuLoadPattern;
		private System.Windows.Forms.MenuItem fileMenuSavePattern;
		private System.Windows.Forms.Button deleteLayerBtn;
		private System.Windows.Forms.Label label8;
		private System.Windows.Forms.Label label9;
		private System.Windows.Forms.Label label10;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.Label label7;
		private System.Windows.Forms.GroupBox groupBox2;
		private System.Windows.Forms.CheckBox timeAsSeedCheckBox;
		private System.Windows.Forms.CheckBox optimalRangeCheckBox;
		private System.Windows.Forms.Button randomOptionsDefaultBtn;
		private System.Windows.Forms.ColumnHeader realOutValue;
		private System.Windows.Forms.ComboBox featureExtraktioncomboBox;
		private System.Windows.Forms.Label label11;
		private System.Windows.Forms.NumericUpDown rangeMinUpDown;
		private System.Windows.Forms.NumericUpDown rangeMaxUpDown;
		private System.Windows.Forms.NumericUpDown seedUpDown;
		private System.Windows.Forms.Label label12;
		private System.Windows.Forms.Label label13;
		private System.Windows.Forms.PictureBox netPictureBox;
		private System.Windows.Forms.RadioButton manualModeRadioBtn;
		private System.Windows.Forms.RadioButton ocrModeRadioBtn;
		private System.Windows.Forms.Label label14;
		private System.Windows.Forms.Label label15;
		private System.Windows.Forms.Label label16;
		private System.Windows.Forms.Label label17;
		private System.Windows.Forms.GroupBox groupBox3;
		private System.Windows.Forms.NumericUpDown noisyImagesNumUpDown;
		private System.Windows.Forms.DomainUpDown letterRangeMinUpDown;
		private System.Windows.Forms.DomainUpDown letterRangeMaxUpDown;
		private System.Windows.Forms.PictureBox charPB;
		private System.Windows.Forms.DomainUpDown charsBmpUpDown;
		private System.Windows.Forms.Button startGeneratePatternsBtn;
		private System.Windows.Forms.Button stopGeneratePatternsBtn;
		private System.Windows.Forms.Label picNumbLab;
		private System.Windows.Forms.GroupBox featureExtractGroupBox;
		private System.Windows.Forms.NumericUpDown dxUpDown;
		private System.Windows.Forms.Label dxLab;
		private System.Windows.Forms.NumericUpDown dyUpDown;
		private System.Windows.Forms.Label dyLab;
		private System.Windows.Forms.GroupBox counterGroupBox;
		private System.Windows.Forms.GroupBox segmGroupBox;
		private System.Windows.Forms.NumericUpDown segmUpDown;
		private System.Windows.Forms.Label segmLab;
		private System.Windows.Forms.NumericUpDown maxErrorUpDown;
		private System.Windows.Forms.Label maxErrorLab;
		private System.Windows.Forms.Button insertLayerBeforeBtn;
		private System.Windows.Forms.Button insertLayerAfterBtn;
		private System.Windows.Forms.GroupBox groupBox5;
		private System.Windows.Forms.GroupBox windowgroupBox;
		private System.Windows.Forms.Label xLab;
		private System.Windows.Forms.NumericUpDown xnUpDown;
		private System.Windows.Forms.NumericUpDown ynUpDown;
		private System.Windows.Forms.Label yLab;
		private System.Windows.Forms.NumericUpDown widthnUpDown;
		private System.Windows.Forms.Label widthLab;
		private System.Windows.Forms.NumericUpDown heightnUpDown;
		private System.Windows.Forms.Label heightLab;
		private System.Windows.Forms.NumericUpDown highThreshnUpDown;
		private System.Windows.Forms.NumericUpDown lowThreshnUpDown;
		private System.Windows.Forms.NumericUpDown sigmanUpDown;
		private System.Windows.Forms.Label sigmaLab;
		private System.Windows.Forms.Label lowThreshLab;
		private System.Windows.Forms.Label highThreshLab;
		private System.Windows.Forms.CheckBox onCannyCheckBox;
		private System.Windows.Forms.RadioButton filterRadioBtn;
		private System.Windows.Forms.RadioButton creationRadioBtn;
		private System.Windows.Forms.CheckBox onGaussCheckBox;
		private System.Windows.Forms.Button selectFontBtn;
		private System.Windows.Forms.FontDialog fontDialog;
		private System.Windows.Forms.CheckBox onGrayCheckBox;
		private System.Windows.Forms.CheckBox onBrightnCheckBox;
		private System.Windows.Forms.CheckBox onHistCheckBox;
		private System.Windows.Forms.PictureBox filteredImgPB;
		private System.Windows.Forms.Label filteredImgLab;
		private System.Windows.Forms.Label top10_2_Text;
		private System.Windows.Forms.Label top10_3_Text;
		private System.Windows.Forms.Label top10_4_Text;
		private System.Windows.Forms.Label top10_5_Text;
		private System.Windows.Forms.Label top10_6_Text;
		private System.Windows.Forms.Label top10_7_Text;
		private System.Windows.Forms.Label top10_8_Text;
		private System.Windows.Forms.Label top10_9_Text;
		private System.Windows.Forms.Label top10_10_Text;
		private System.Windows.Forms.Label top10_1_Text;
		private System.Windows.Forms.ProgressBar top10_10_progBar;
		private System.Windows.Forms.ProgressBar top10_1_progBar;
		private System.Windows.Forms.CheckBox assoziateNwLCB;
		private System.Windows.Forms.ProgressBar top10_3_progBar;
		private System.Windows.Forms.ProgressBar top10_9_progBar;
		private System.Windows.Forms.ProgressBar top10_7_progBar;
		private System.Windows.Forms.ProgressBar top10_2_progBar;
		private System.Windows.Forms.ProgressBar top10_8_progBar;
		private System.Windows.Forms.ProgressBar top10_6_progBar;
		private System.Windows.Forms.ProgressBar top10_5_progBar;
		private System.Windows.Forms.ProgressBar top10_4_progBar;
		private System.Windows.Forms.MenuItem netMenuStartStopThreadItem;
		private System.Windows.Forms.StatusBarPanel memUse;
		private System.Windows.Forms.MenuItem infoMenuHelpItem;
		private System.Windows.Forms.GroupBox top10_GroupBox;
		private ParamAdjuster.ParamAdjuster noiseAdjuster;
		private ParamAdjuster.ParamAdjuster steepnessAdjuster;
		private ParamAdjuster.ParamAdjuster slowmoAdjuster;
		private System.Windows.Forms.CheckBox generateNetcheckBox;
		private System.Windows.Forms.GroupBox sepImgGroupBox;
		private System.Windows.Forms.GroupBox cannyGroupBox;
		private System.Windows.Forms.CheckBox onBinCheckBox;
		private System.Windows.Forms.Label binThreshLab;
		private System.Windows.Forms.NumericUpDown binUpDown;
		private System.Windows.Forms.GroupBox scaleGroupBox;
		private System.Windows.Forms.Label heightScaleLab;
		private System.Windows.Forms.Label widthScaleLab;
		private System.Windows.Forms.NumericUpDown heightScalenUpDown;
		private System.Windows.Forms.NumericUpDown widthScalenUpDown;
		private System.Windows.Forms.GroupBox segmRgnGroupBox;
		private System.Windows.Forms.NumericUpDown heightCharRgnnUpDown;
		private System.Windows.Forms.Label heightCharRgnLab;
		private System.Windows.Forms.NumericUpDown widthCharRgnnUpDown;
		private System.Windows.Forms.Label widthCharRgnLab;
		private System.Windows.Forms.NumericUpDown yCharRgnnUpDown;
		private System.Windows.Forms.Label yCharRgnLab;
		private System.Windows.Forms.NumericUpDown xCharRgnnUpDown;
		private System.Windows.Forms.Label xCharRgnLab;
		private System.Windows.Forms.Label threshVertLab;
		private System.Windows.Forms.Label threshHorizLab;
		private System.Windows.Forms.GroupBox imgSepGroupBox;
		private System.Windows.Forms.Button startExtractBtn;
		private System.Windows.Forms.Button stopExtractBtn;
		private System.Windows.Forms.NumericUpDown imgSepThreshHoriznUpDown;
		private System.Windows.Forms.NumericUpDown imgSepThreshVertnUpDown;
		private System.Windows.Forms.Button delImgBtn;
		private System.Windows.Forms.Button changeInputPatternBtn;
		private System.Windows.Forms.CheckBox addPatsCheckBox;
		private System.ComponentModel.IContainer components;
        #endregion

        #endregion

        #region Methoden 

        [STAThread]
        static void Main(string[] args)
		{
			try
			{	
			if(args.Length > 0)
				Application.Run(new GUI(args[0]));
			else
				Application.Run(new GUI());
			}
			catch(Exception exc)
			{
				handleException(exc);
				return;
			}
		}


		# region GUI Init.

		public GUI()
		{
			this.initGUI();
			fileMenuNewItem.PerformClick();
			initDesigner();
			isDirty = false;
		}

		public GUI(string pathToNetFile)
		{
			this.initGUI();
			loadNetAndSetCtrls(pathToNetFile);
			initDesigner();
			isDirty = false;
		}

		private void initGUI()
		{
			InitializeComponent();

		    openFileDialogNN.InitialDirectory = System.IO.Path.GetFullPath(openFileDialogNN.InitialDirectory);
		    saveFileDialogNN.InitialDirectory = System.IO.Path.GetFullPath(saveFileDialogNN.InitialDirectory);
		    openFileDialogPattern.InitialDirectory = System.IO.Path.GetFullPath(openFileDialogPattern.InitialDirectory);
            saveFileDialogPattern.InitialDirectory = System.IO.Path.GetFullPath(saveFileDialogPattern.InitialDirectory);
            openFileDialogPic.InitialDirectory = System.IO.Path.GetFullPath(openFileDialogPic.InitialDirectory);

		    Control.CheckForIllegalCrossThreadCalls = false;
			
			t = new System.Windows.Forms.Timer();
			t.Interval = 5000;
			t.Tick += new EventHandler(t_Tick);
			t.Start();
			t_Tick(null, null);

			formTitle = ".: " + Application.ProductName + " :.";			
			pen = new Pen(Color.Red);
			axisFont = new Font("_sans", 8);
			axisBrush = new SolidBrush(Color.Blue);
			arrowPen = new Pen(Color.BlueViolet, 5f);
			arrowPen.EndCap = System.Drawing.Drawing2D.LineCap.ArrowAnchor;

			initTrainer();
			initPatternBuilder();
							
			mainTabCtrl.SelectedTab = designPage;
		}

		private void initDesigner()
		{
			this.actFuncs = new IActivationFunction[]
			{
				new LogisticActFunc(),
				new TanhActFunc(),
				new StepActFunc()
			};
			for(int i=0; i<actFuncs.Length; i++)
			{
				this.activationFuncComboBox.Items.Add(this.actFuncs[i].GetType().GetProperty("Name").GetValue(actFuncs[i],null));
				if(this.actFuncs[i].GetType() == nn.layersProp[0].neuronsProp[0].actFuncProp.GetType())
					this.activationFuncComboBox.SelectedIndex = i;
			}
			this.activationFuncComboBox.SelectedIndexChanged += new System.EventHandler(this.ActivationFunctionComboBox_SelectedIndexChanged);

			this.setRandomOptions();

			this.useBestRange = true;
			this.useTimeAsSeed = true;

			if(useTimeAsSeed)
				this.timeAsSeedCheckBox.Checked = true;
			else
				this.timeAsSeedCheckBox.Checked = false;

			if(useBestRange)
				this.optimalRangeCheckBox.Checked = true;
			else
				this.optimalRangeCheckBox.Checked = false;

			this.netNameTxt.Text = nn.netNameProp;
			this.showNeuralNet();
			this.drawNetTopology();
		}

		private void initPatternBuilder()
		{
			this.extractFuncs = new String[,]
			{
				{ "Count black pixels in rows and cols", "OCRPreProcessing.BlackPxCounter" },
				{ "Count and add black pixels in rows and cols", "OCRPreProcessing.BlackPxAddRowAndColCounter" },
				{ "Count black/white changes in rows and cols", "OCRPreProcessing.BlackPxWhitePxChangesCounter" },
				{ "Count black pixels in image segments", "OCRPreProcessing.ImgSegmenter" },
				{ "Raw data -> brightness of every pixel", "OCRPreProcessing.RawExtractor" }
			};
			this.onOCRCharComputing += new OCRPreProcessing.progressHandler(calcProgressBar);

			// t-bä 09.10.
			progBars = new ProgressBar[]
				{
					this.top10_1_progBar,
					this.top10_2_progBar,
					this.top10_3_progBar,
					this.top10_4_progBar,
					this.top10_5_progBar,
					this.top10_6_progBar,
					this.top10_7_progBar,
					this.top10_8_progBar,
					this.top10_9_progBar,
					this.top10_10_progBar
				};

			labels = new Label[]
				{
					this.top10_1_Text,
					this.top10_2_Text,
					this.top10_3_Text,
					this.top10_4_Text,
					this.top10_5_Text,
					this.top10_6_Text,
					this.top10_7_Text,
					this.top10_8_Text,
					this.top10_9_Text,
					this.top10_10_Text
				};

			for(int i=0; i<this.extractFuncs.GetUpperBound(0)+1; i++)
			{
				featureExtraktioncomboBox.Items.Add(extractFuncs[i,0]);
			}
			featureExtraktioncomboBox.SelectedIndex = 0;

			this.letterRangeMinUpDown.SelectedIndex = 0;
			this.letterRangeMaxUpDown.SelectedIndex = this.letterRangeMaxUpDown.Items.Count-1;

			imgFilters = new IImageFilter[6];
			imgFilters[0] = new GrayFilter();
			imgFilters[1] = new BrightnNormalizer();
			imgFilters[2] = new HistEqualizer();
			imgFilters[3] = new GaussFilter();
			imgFilters[4] = new CannyFilter();
			imgFilters[5] = new BinarizeFilter();
			sigmanUpDown.Value = (decimal)(imgFilters[3] as GaussFilter).sigmaProp;
			(imgFilters[4] as CannyFilter).doSmoothingProp = false;
			lowThreshnUpDown.Value = (decimal)(imgFilters[4] as CannyFilter).lowThresholdProp;
			highThreshnUpDown.Value = (decimal)(imgFilters[4] as CannyFilter).highThresholdProp;
			binUpDown.Value = (decimal)(imgFilters[5] as BinarizeFilter).thresholdProp;

			for(int i=0; i<imgFilters.Length; i++)
				imgFilters[i].onComputing += new OCRPreProcessing.progressHandler(calcProgressBar);

			imgSeparator = new HistogramSeparator();
			imgSepThreshHoriznUpDown.Value = (decimal)imgSeparator.LineThreshold;
			imgSepThreshVertnUpDown.Value = (decimal)imgSeparator.ColumnThreshold;
			imgSeparator.onComputing += new OCRPreProcessing.progressHandler(calcProgressBar);

			OCRImageCreator creator = new OCRImageCreator('W');
			setCtrlsToBmp(creator.bmpProp, true);
			fontDialog.Font = creator.fontProp;
		}

		private void initTrainer()
		{
			visualizeErrorProgress = new errorHandler(calcErrorGraph);
			// t-bä
			//	displayGeneration = new LearningAlgo.handleException(showGeneration);
			//r-s
//			checkLastErrorsAndCtrlLearning = new MultilayerNet.errorHandler(automatedLearning);
			//r-s
			lastErrors = new Queue(QUEUE_SIZE);
			//r-s
		}

		#endregion

		/// <summary>
		/// Die verwendeten Ressourcen bereinigen.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if(components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}
		#region Vom Windows Form-Designer generierter Code
		/// <summary>
		/// Erforderliche Methode für die Designerunterstützung. 
		/// Der Inhalt der Methode darf nicht mit dem Code-Editor geändert werden.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(GUI));
			this.statusBar = new System.Windows.Forms.StatusBar();
			this.infoSBP = new System.Windows.Forms.StatusBarPanel();
			this.progressSBP = new System.Windows.Forms.StatusBarPanel();
			this.percentSBP = new System.Windows.Forms.StatusBarPanel();
			this.timeSBP = new System.Windows.Forms.StatusBarPanel();
			this.remainingSBP = new System.Windows.Forms.StatusBarPanel();
			this.memUse = new System.Windows.Forms.StatusBarPanel();
			this.errorPB = new System.Windows.Forms.PictureBox();
			this.errorGraphLabel = new System.Windows.Forms.Label();
			this.errorListViewLabel = new System.Windows.Forms.Label();
			this.errorListView = new System.Windows.Forms.ListView();
			this.CycleHeader = new System.Windows.Forms.ColumnHeader();
			this.ErrorHeader = new System.Windows.Forms.ColumnHeader();
			this.mainTabCtrl = new System.Windows.Forms.TabControl();
			this.designPage = new System.Windows.Forms.TabPage();
			this.insertLayerAfterBtn = new System.Windows.Forms.Button();
			this.netPictureBox = new System.Windows.Forms.PictureBox();
			this.label13 = new System.Windows.Forms.Label();
			this.label12 = new System.Windows.Forms.Label();
			this.groupBox2 = new System.Windows.Forms.GroupBox();
			this.label6 = new System.Windows.Forms.Label();
			this.activationFuncComboBox = new System.Windows.Forms.ComboBox();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.randomOptionsDefaultBtn = new System.Windows.Forms.Button();
			this.label7 = new System.Windows.Forms.Label();
			this.label10 = new System.Windows.Forms.Label();
			this.label8 = new System.Windows.Forms.Label();
			this.label9 = new System.Windows.Forms.Label();
			this.timeAsSeedCheckBox = new System.Windows.Forms.CheckBox();
			this.optimalRangeCheckBox = new System.Windows.Forms.CheckBox();
			this.rangeMaxUpDown = new System.Windows.Forms.NumericUpDown();
			this.rangeMinUpDown = new System.Windows.Forms.NumericUpDown();
			this.seedUpDown = new System.Windows.Forms.NumericUpDown();
			this.netNameTxt = new System.Windows.Forms.TextBox();
			this.label4 = new System.Windows.Forms.Label();
			this.deleteLayerBtn = new System.Windows.Forms.Button();
			this.netTreeCollapse_Btn = new System.Windows.Forms.Button();
			this.netTreeExpand_Btn = new System.Windows.Forms.Button();
			this.generateNetBtn = new System.Windows.Forms.Button();
			this.insertLayerBeforeBtn = new System.Windows.Forms.Button();
			this.neuronsNumTxt = new System.Windows.Forms.TextBox();
			this.clearBtn = new System.Windows.Forms.Button();
			this.showNetBtn = new System.Windows.Forms.Button();
			this.netTreeView = new System.Windows.Forms.TreeView();
			this.label3 = new System.Windows.Forms.Label();
			this.patternPage = new System.Windows.Forms.TabPage();
			this.patternModeGroupBox = new System.Windows.Forms.GroupBox();
			this.manualModeRadioBtn = new System.Windows.Forms.RadioButton();
			this.ocrModeRadioBtn = new System.Windows.Forms.RadioButton();
			this.manualModePanel = new System.Windows.Forms.Panel();
			this.top10_GroupBox = new System.Windows.Forms.GroupBox();
			this.top10_2_Text = new System.Windows.Forms.Label();
			this.top10_3_Text = new System.Windows.Forms.Label();
			this.top10_4_Text = new System.Windows.Forms.Label();
			this.top10_5_Text = new System.Windows.Forms.Label();
			this.top10_6_Text = new System.Windows.Forms.Label();
			this.top10_7_Text = new System.Windows.Forms.Label();
			this.top10_8_Text = new System.Windows.Forms.Label();
			this.top10_9_Text = new System.Windows.Forms.Label();
			this.top10_10_Text = new System.Windows.Forms.Label();
			this.top10_1_Text = new System.Windows.Forms.Label();
			this.top10_10_progBar = new System.Windows.Forms.ProgressBar();
			this.top10_1_progBar = new System.Windows.Forms.ProgressBar();
			this.assoziateNwLCB = new System.Windows.Forms.CheckBox();
			this.top10_3_progBar = new System.Windows.Forms.ProgressBar();
			this.top10_9_progBar = new System.Windows.Forms.ProgressBar();
			this.top10_7_progBar = new System.Windows.Forms.ProgressBar();
			this.top10_2_progBar = new System.Windows.Forms.ProgressBar();
			this.top10_8_progBar = new System.Windows.Forms.ProgressBar();
			this.top10_6_progBar = new System.Windows.Forms.ProgressBar();
			this.top10_5_progBar = new System.Windows.Forms.ProgressBar();
			this.top10_4_progBar = new System.Windows.Forms.ProgressBar();
			this.patternTreeCollapse_Btn = new System.Windows.Forms.Button();
			this.patternTreeExpand_Btn = new System.Windows.Forms.Button();
			this.label5 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.label1 = new System.Windows.Forms.Label();
			this.changeOutputPatternBtn = new System.Windows.Forms.Button();
			this.patternTreeView = new System.Windows.Forms.TreeView();
			this.patternOutputListView = new System.Windows.Forms.ListView();
			this.OutputNeuron = new System.Windows.Forms.ColumnHeader();
			this.OutputValue = new System.Windows.Forms.ColumnHeader();
			this.realOutValue = new System.Windows.Forms.ColumnHeader();
			this.deletePatternBtn = new System.Windows.Forms.Button();
			this.newPatternBtn = new System.Windows.Forms.Button();
			this.clearPaternsBtn = new System.Windows.Forms.Button();
			this.currentPatternBtn = new System.Windows.Forms.Button();
			this.changeInputPatternBtn = new System.Windows.Forms.Button();
			this.outputTxt = new System.Windows.Forms.TextBox();
			this.inputTxt = new System.Windows.Forms.TextBox();
			this.patternInputListView = new System.Windows.Forms.ListView();
			this.InputNeuron = new System.Windows.Forms.ColumnHeader();
			this.InputValue = new System.Windows.Forms.ColumnHeader();
			this.deleteAllPaternsBtn = new System.Windows.Forms.Button();
			this.ocrModePanel = new System.Windows.Forms.Panel();
			this.imgSepGroupBox = new System.Windows.Forms.GroupBox();
			this.imgSepThreshHoriznUpDown = new System.Windows.Forms.NumericUpDown();
			this.imgSepThreshVertnUpDown = new System.Windows.Forms.NumericUpDown();
			this.threshVertLab = new System.Windows.Forms.Label();
			this.threshHorizLab = new System.Windows.Forms.Label();
			this.startExtractBtn = new System.Windows.Forms.Button();
			this.stopExtractBtn = new System.Windows.Forms.Button();
			this.sepImgGroupBox = new System.Windows.Forms.GroupBox();
			this.addPatsCheckBox = new System.Windows.Forms.CheckBox();
			this.delImgBtn = new System.Windows.Forms.Button();
			this.charsBmpUpDown = new System.Windows.Forms.DomainUpDown();
			this.picNumbLab = new System.Windows.Forms.Label();
			this.charPB = new System.Windows.Forms.PictureBox();
			this.windowgroupBox = new System.Windows.Forms.GroupBox();
			this.heightnUpDown = new System.Windows.Forms.NumericUpDown();
			this.heightLab = new System.Windows.Forms.Label();
			this.widthnUpDown = new System.Windows.Forms.NumericUpDown();
			this.widthLab = new System.Windows.Forms.Label();
			this.ynUpDown = new System.Windows.Forms.NumericUpDown();
			this.yLab = new System.Windows.Forms.Label();
			this.xnUpDown = new System.Windows.Forms.NumericUpDown();
			this.xLab = new System.Windows.Forms.Label();
			this.scaleGroupBox = new System.Windows.Forms.GroupBox();
			this.heightScalenUpDown = new System.Windows.Forms.NumericUpDown();
			this.heightScaleLab = new System.Windows.Forms.Label();
			this.widthScalenUpDown = new System.Windows.Forms.NumericUpDown();
			this.widthScaleLab = new System.Windows.Forms.Label();
			this.generateNetcheckBox = new System.Windows.Forms.CheckBox();
			this.creationRadioBtn = new System.Windows.Forms.RadioButton();
			this.groupBox5 = new System.Windows.Forms.GroupBox();
			this.onGrayCheckBox = new System.Windows.Forms.CheckBox();
			this.srcImgLab = new System.Windows.Forms.Label();
			this.srcImgPB = new System.Windows.Forms.PictureBox();
			this.filteredImgPB = new System.Windows.Forms.PictureBox();
			this.filteredImgLab = new System.Windows.Forms.Label();
			this.cannyGroupBox = new System.Windows.Forms.GroupBox();
			this.onGaussCheckBox = new System.Windows.Forms.CheckBox();
			this.onCannyCheckBox = new System.Windows.Forms.CheckBox();
			this.highThreshnUpDown = new System.Windows.Forms.NumericUpDown();
			this.lowThreshnUpDown = new System.Windows.Forms.NumericUpDown();
			this.highThreshLab = new System.Windows.Forms.Label();
			this.lowThreshLab = new System.Windows.Forms.Label();
			this.sigmanUpDown = new System.Windows.Forms.NumericUpDown();
			this.sigmaLab = new System.Windows.Forms.Label();
			this.onBrightnCheckBox = new System.Windows.Forms.CheckBox();
			this.onHistCheckBox = new System.Windows.Forms.CheckBox();
			this.onBinCheckBox = new System.Windows.Forms.CheckBox();
			this.binThreshLab = new System.Windows.Forms.Label();
			this.binUpDown = new System.Windows.Forms.NumericUpDown();
			this.segmRgnGroupBox = new System.Windows.Forms.GroupBox();
			this.heightCharRgnnUpDown = new System.Windows.Forms.NumericUpDown();
			this.heightCharRgnLab = new System.Windows.Forms.Label();
			this.widthCharRgnnUpDown = new System.Windows.Forms.NumericUpDown();
			this.widthCharRgnLab = new System.Windows.Forms.Label();
			this.yCharRgnnUpDown = new System.Windows.Forms.NumericUpDown();
			this.yCharRgnLab = new System.Windows.Forms.Label();
			this.xCharRgnnUpDown = new System.Windows.Forms.NumericUpDown();
			this.xCharRgnLab = new System.Windows.Forms.Label();
			this.featureExtractGroupBox = new System.Windows.Forms.GroupBox();
			this.label11 = new System.Windows.Forms.Label();
			this.featureExtraktioncomboBox = new System.Windows.Forms.ComboBox();
			this.counterGroupBox = new System.Windows.Forms.GroupBox();
			this.dyUpDown = new System.Windows.Forms.NumericUpDown();
			this.dyLab = new System.Windows.Forms.Label();
			this.dxUpDown = new System.Windows.Forms.NumericUpDown();
			this.dxLab = new System.Windows.Forms.Label();
			this.segmGroupBox = new System.Windows.Forms.GroupBox();
			this.segmLab = new System.Windows.Forms.Label();
			this.segmUpDown = new System.Windows.Forms.NumericUpDown();
			this.groupBox3 = new System.Windows.Forms.GroupBox();
			this.noiseAdjuster = new ParamAdjuster.ParamAdjuster();
			this.selectFontBtn = new System.Windows.Forms.Button();
			this.noisyImagesNumUpDown = new System.Windows.Forms.NumericUpDown();
			this.label17 = new System.Windows.Forms.Label();
			this.label16 = new System.Windows.Forms.Label();
			this.label15 = new System.Windows.Forms.Label();
			this.label14 = new System.Windows.Forms.Label();
			this.letterRangeMinUpDown = new System.Windows.Forms.DomainUpDown();
			this.letterRangeMaxUpDown = new System.Windows.Forms.DomainUpDown();
			this.filterRadioBtn = new System.Windows.Forms.RadioButton();
			this.startGeneratePatternsBtn = new System.Windows.Forms.Button();
			this.stopGeneratePatternsBtn = new System.Windows.Forms.Button();
			this.trainPage = new System.Windows.Forms.TabPage();
			this.slowmoAdjuster = new ParamAdjuster.ParamAdjuster();
			this.steepnessAdjuster = new ParamAdjuster.ParamAdjuster();
			this.maxErrorUpDown = new System.Windows.Forms.NumericUpDown();
			this.maxErrorLab = new System.Windows.Forms.Label();
			this.cyclesUpDown = new System.Windows.Forms.NumericUpDown();
			this.cyclesLabel = new System.Windows.Forms.Label();
			this.learnAlgoLabel = new System.Windows.Forms.Label();
			this.learnAlgoComboBox = new System.Windows.Forms.ComboBox();
			this.stopTrainBtn = new System.Windows.Forms.Button();
			this.fastModeCheckBox = new System.Windows.Forms.CheckBox();
			this.startTrainBtn = new System.Windows.Forms.Button();
			this.fastModeLabel = new System.Windows.Forms.Label();
			this.currentErrorTxt = new System.Windows.Forms.Label();
			this.mainMenu = new System.Windows.Forms.MainMenu();
			this.fileMenuItem = new System.Windows.Forms.MenuItem();
			this.fileMenuNewItem = new System.Windows.Forms.MenuItem();
			this.fileMenuLoadItem = new System.Windows.Forms.MenuItem();
			this.fileMenuSaveItem = new System.Windows.Forms.MenuItem();
			this.fileMenuItemStrich1 = new System.Windows.Forms.MenuItem();
			this.fileMenuLoadPattern = new System.Windows.Forms.MenuItem();
			this.fileMenuSavePattern = new System.Windows.Forms.MenuItem();
			this.menuItem1 = new System.Windows.Forms.MenuItem();
			this.fileMenuLoadPicItem = new System.Windows.Forms.MenuItem();
			this.menuItem2 = new System.Windows.Forms.MenuItem();
			this.fileMenuExitItem = new System.Windows.Forms.MenuItem();
			this.netMenuItem = new System.Windows.Forms.MenuItem();
			this.netMenuRandomizeItem = new System.Windows.Forms.MenuItem();
			this.netMenuStartStopThreadItem = new System.Windows.Forms.MenuItem();
			this.infoMenuItem = new System.Windows.Forms.MenuItem();
			this.infoMenuHelpItem = new System.Windows.Forms.MenuItem();
			this.infoMenuAboutItem = new System.Windows.Forms.MenuItem();
			this.openFileDialogNN = new System.Windows.Forms.OpenFileDialog();
			this.saveFileDialogNN = new System.Windows.Forms.SaveFileDialog();
			this.openFileDialogPic = new System.Windows.Forms.OpenFileDialog();
			this.toolTip = new System.Windows.Forms.ToolTip(this.components);
			this.batchLearnRadioButton = new System.Windows.Forms.RadioButton();
			this.openFileDialogPattern = new System.Windows.Forms.OpenFileDialog();
			this.saveFileDialogPattern = new System.Windows.Forms.SaveFileDialog();
			this.fontDialog = new System.Windows.Forms.FontDialog();
			((System.ComponentModel.ISupportInitialize)(this.infoSBP)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.progressSBP)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.percentSBP)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.timeSBP)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.remainingSBP)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.memUse)).BeginInit();
			this.mainTabCtrl.SuspendLayout();
			this.designPage.SuspendLayout();
			this.groupBox2.SuspendLayout();
			this.groupBox1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.rangeMaxUpDown)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.rangeMinUpDown)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.seedUpDown)).BeginInit();
			this.patternPage.SuspendLayout();
			this.patternModeGroupBox.SuspendLayout();
			this.manualModePanel.SuspendLayout();
			this.top10_GroupBox.SuspendLayout();
			this.ocrModePanel.SuspendLayout();
			this.imgSepGroupBox.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.imgSepThreshHoriznUpDown)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.imgSepThreshVertnUpDown)).BeginInit();
			this.sepImgGroupBox.SuspendLayout();
			this.windowgroupBox.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.heightnUpDown)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.widthnUpDown)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.ynUpDown)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.xnUpDown)).BeginInit();
			this.scaleGroupBox.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.heightScalenUpDown)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.widthScalenUpDown)).BeginInit();
			this.groupBox5.SuspendLayout();
			this.cannyGroupBox.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.highThreshnUpDown)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.lowThreshnUpDown)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.sigmanUpDown)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.binUpDown)).BeginInit();
			this.segmRgnGroupBox.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.heightCharRgnnUpDown)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.widthCharRgnnUpDown)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.yCharRgnnUpDown)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.xCharRgnnUpDown)).BeginInit();
			this.featureExtractGroupBox.SuspendLayout();
			this.counterGroupBox.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.dyUpDown)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.dxUpDown)).BeginInit();
			this.segmGroupBox.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.segmUpDown)).BeginInit();
			this.groupBox3.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.noisyImagesNumUpDown)).BeginInit();
			this.trainPage.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.maxErrorUpDown)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.cyclesUpDown)).BeginInit();
			this.SuspendLayout();
			// 
			// statusBar
			// 
			this.statusBar.Location = new System.Drawing.Point(0, 621);
			this.statusBar.Name = "statusBar";
			this.statusBar.Panels.AddRange(new System.Windows.Forms.StatusBarPanel[] {
																						 this.infoSBP,
																						 this.progressSBP,
																						 this.percentSBP,
																						 this.timeSBP,
																						 this.remainingSBP,
																						 this.memUse});
			this.statusBar.ShowPanels = true;
			this.statusBar.Size = new System.Drawing.Size(952, 16);
			this.statusBar.TabIndex = 6;
			this.statusBar.DrawItem += new System.Windows.Forms.StatusBarDrawItemEventHandler(this.statusBar_DrawItem);
			// 
			// infoSBP
			// 
			this.infoSBP.AutoSize = System.Windows.Forms.StatusBarPanelAutoSize.Contents;
			this.infoSBP.MinWidth = 250;
			this.infoSBP.Text = "Welcome neural network friend! :o)";
			this.infoSBP.ToolTipText = "Progresstime";
			this.infoSBP.Width = 250;
			// 
			// progressSBP
			// 
			this.progressSBP.MinWidth = 100;
			this.progressSBP.Style = System.Windows.Forms.StatusBarPanelStyle.OwnerDraw;
			this.progressSBP.Text = "0%";
			// 
			// percentSBP
			// 
			this.percentSBP.MinWidth = 40;
			this.percentSBP.Text = "0 %";
			this.percentSBP.Width = 40;
			// 
			// timeSBP
			// 
			this.timeSBP.MinWidth = 165;
			this.timeSBP.Width = 165;
			// 
			// remainingSBP
			// 
			this.remainingSBP.MinWidth = 140;
			this.remainingSBP.Width = 140;
			// 
			// memUse
			// 
			this.memUse.Alignment = System.Windows.Forms.HorizontalAlignment.Right;
			this.memUse.AutoSize = System.Windows.Forms.StatusBarPanelAutoSize.Contents;
			this.memUse.Width = 10;
			// 
			// errorPB
			// 
			this.errorPB.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
				| System.Windows.Forms.AnchorStyles.Left) 
				| System.Windows.Forms.AnchorStyles.Right)));
			this.errorPB.BackColor = System.Drawing.SystemColors.Window;
			this.errorPB.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.errorPB.Cursor = System.Windows.Forms.Cursors.Cross;
			this.errorPB.Location = new System.Drawing.Point(312, 32);
			this.errorPB.Name = "errorPB";
			this.errorPB.Size = new System.Drawing.Size(616, 432);
			this.errorPB.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
			this.errorPB.TabIndex = 9;
			this.errorPB.TabStop = false;
			this.errorPB.Paint += new System.Windows.Forms.PaintEventHandler(this.errorPB_Paint);
			this.errorPB.MouseMove += new System.Windows.Forms.MouseEventHandler(this.errorPB_MouseMove);
			// 
			// errorGraphLabel
			// 
			this.errorGraphLabel.Location = new System.Drawing.Point(312, 16);
			this.errorGraphLabel.Name = "errorGraphLabel";
			this.errorGraphLabel.Size = new System.Drawing.Size(184, 16);
			this.errorGraphLabel.TabIndex = 10;
			this.errorGraphLabel.Text = "Graph of the global summed error:";
			// 
			// errorListViewLabel
			// 
			this.errorListViewLabel.Location = new System.Drawing.Point(8, 16);
			this.errorListViewLabel.Name = "errorListViewLabel";
			this.errorListViewLabel.Size = new System.Drawing.Size(184, 16);
			this.errorListViewLabel.TabIndex = 12;
			this.errorListViewLabel.Text = "Global summed error progress:";
			// 
			// errorListView
			// 
			this.errorListView.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
				| System.Windows.Forms.AnchorStyles.Left)));
			this.errorListView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
																							this.CycleHeader,
																							this.ErrorHeader});
			this.errorListView.FullRowSelect = true;
			this.errorListView.Location = new System.Drawing.Point(8, 32);
			this.errorListView.MultiSelect = false;
			this.errorListView.Name = "errorListView";
			this.errorListView.Size = new System.Drawing.Size(296, 533);
			this.errorListView.TabIndex = 76;
			this.errorListView.View = System.Windows.Forms.View.Details;
			// 
			// CycleHeader
			// 
			this.CycleHeader.Text = "Cycle";
			// 
			// ErrorHeader
			// 
			this.ErrorHeader.Text = "Error";
			this.ErrorHeader.Width = 85;
			// 
			// mainTabCtrl
			// 
			this.mainTabCtrl.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
				| System.Windows.Forms.AnchorStyles.Left) 
				| System.Windows.Forms.AnchorStyles.Right)));
			this.mainTabCtrl.Controls.Add(this.designPage);
			this.mainTabCtrl.Controls.Add(this.patternPage);
			this.mainTabCtrl.Controls.Add(this.trainPage);
			this.mainTabCtrl.Location = new System.Drawing.Point(0, 0);
			this.mainTabCtrl.Name = "mainTabCtrl";
			this.mainTabCtrl.SelectedIndex = 0;
			this.mainTabCtrl.Size = new System.Drawing.Size(952, 616);
			this.mainTabCtrl.TabIndex = 18;
			this.mainTabCtrl.SelectedIndexChanged += new System.EventHandler(this.mainTabCtrl_SelectedIndexChanged);
			// 
			// designPage
			// 
			this.designPage.Controls.Add(this.insertLayerAfterBtn);
			this.designPage.Controls.Add(this.netPictureBox);
			this.designPage.Controls.Add(this.label13);
			this.designPage.Controls.Add(this.label12);
			this.designPage.Controls.Add(this.groupBox2);
			this.designPage.Controls.Add(this.deleteLayerBtn);
			this.designPage.Controls.Add(this.netTreeCollapse_Btn);
			this.designPage.Controls.Add(this.netTreeExpand_Btn);
			this.designPage.Controls.Add(this.generateNetBtn);
			this.designPage.Controls.Add(this.insertLayerBeforeBtn);
			this.designPage.Controls.Add(this.neuronsNumTxt);
			this.designPage.Controls.Add(this.clearBtn);
			this.designPage.Controls.Add(this.showNetBtn);
			this.designPage.Controls.Add(this.netTreeView);
			this.designPage.Controls.Add(this.label3);
			this.designPage.Location = new System.Drawing.Point(4, 22);
			this.designPage.Name = "designPage";
			this.designPage.Size = new System.Drawing.Size(944, 590);
			this.designPage.TabIndex = 1;
			this.designPage.Text = "Net Designer";
			// 
			// insertLayerAfterBtn
			// 
			this.insertLayerAfterBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.insertLayerAfterBtn.Location = new System.Drawing.Point(480, 528);
			this.insertLayerAfterBtn.Name = "insertLayerAfterBtn";
			this.insertLayerAfterBtn.Size = new System.Drawing.Size(120, 24);
			this.insertLayerAfterBtn.TabIndex = 14;
			this.insertLayerAfterBtn.Text = "Insert after";
			this.toolTip.SetToolTip(this.insertLayerAfterBtn, "insert layer after the selected");
			this.insertLayerAfterBtn.Click += new System.EventHandler(this.insertLayerAfterBtn_Click);
			// 
			// netPictureBox
			// 
			this.netPictureBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
				| System.Windows.Forms.AnchorStyles.Left) 
				| System.Windows.Forms.AnchorStyles.Right)));
			this.netPictureBox.BackColor = System.Drawing.SystemColors.ControlLightLight;
			this.netPictureBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.netPictureBox.Location = new System.Drawing.Point(664, 56);
			this.netPictureBox.Name = "netPictureBox";
			this.netPictureBox.Size = new System.Drawing.Size(264, 391);
			this.netPictureBox.TabIndex = 53;
			this.netPictureBox.TabStop = false;
			this.netPictureBox.Resize += new System.EventHandler(this.netPictureBox_Resize);
			// 
			// label13
			// 
			this.label13.Location = new System.Drawing.Point(336, 40);
			this.label13.Name = "label13";
			this.label13.Size = new System.Drawing.Size(120, 16);
			this.label13.TabIndex = 52;
			this.label13.Text = "Network topology";
			// 
			// label12
			// 
			this.label12.Location = new System.Drawing.Point(664, 40);
			this.label12.Name = "label12";
			this.label12.Size = new System.Drawing.Size(120, 16);
			this.label12.TabIndex = 51;
			this.label12.Text = "Network visualization";
			// 
			// groupBox2
			// 
			this.groupBox2.Controls.Add(this.label6);
			this.groupBox2.Controls.Add(this.activationFuncComboBox);
			this.groupBox2.Controls.Add(this.groupBox1);
			this.groupBox2.Controls.Add(this.netNameTxt);
			this.groupBox2.Controls.Add(this.label4);
			this.groupBox2.Location = new System.Drawing.Point(16, 48);
			this.groupBox2.Name = "groupBox2";
			this.groupBox2.RightToLeft = System.Windows.Forms.RightToLeft.No;
			this.groupBox2.Size = new System.Drawing.Size(264, 400);
			this.groupBox2.TabIndex = 50;
			this.groupBox2.TabStop = false;
			this.groupBox2.Text = "Global network options";
			// 
			// label6
			// 
			this.label6.Location = new System.Drawing.Point(16, 88);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(104, 16);
			this.label6.TabIndex = 38;
			this.label6.Text = "Activation function:";
			// 
			// activationFuncComboBox
			// 
			this.activationFuncComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.activationFuncComboBox.Location = new System.Drawing.Point(16, 104);
			this.activationFuncComboBox.Name = "activationFuncComboBox";
			this.activationFuncComboBox.Size = new System.Drawing.Size(232, 21);
			this.activationFuncComboBox.TabIndex = 1;
			this.toolTip.SetToolTip(this.activationFuncComboBox, "select the activation function for all neurons of the net");
			this.activationFuncComboBox.SelectedIndexChanged += new System.EventHandler(this.ActivationFunctionComboBox_SelectedIndexChanged);
			// 
			// groupBox1
			// 
			this.groupBox1.Controls.Add(this.randomOptionsDefaultBtn);
			this.groupBox1.Controls.Add(this.label7);
			this.groupBox1.Controls.Add(this.label10);
			this.groupBox1.Controls.Add(this.label8);
			this.groupBox1.Controls.Add(this.label9);
			this.groupBox1.Controls.Add(this.timeAsSeedCheckBox);
			this.groupBox1.Controls.Add(this.optimalRangeCheckBox);
			this.groupBox1.Controls.Add(this.rangeMaxUpDown);
			this.groupBox1.Controls.Add(this.rangeMinUpDown);
			this.groupBox1.Controls.Add(this.seedUpDown);
			this.groupBox1.Location = new System.Drawing.Point(16, 184);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(232, 192);
			this.groupBox1.TabIndex = 49;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "Randomize options";
			// 
			// randomOptionsDefaultBtn
			// 
			this.randomOptionsDefaultBtn.Location = new System.Drawing.Point(16, 160);
			this.randomOptionsDefaultBtn.Name = "randomOptionsDefaultBtn";
			this.randomOptionsDefaultBtn.Size = new System.Drawing.Size(200, 24);
			this.randomOptionsDefaultBtn.TabIndex = 7;
			this.randomOptionsDefaultBtn.Text = "Default";
			this.toolTip.SetToolTip(this.randomOptionsDefaultBtn, "reset seed and range to default values");
			this.randomOptionsDefaultBtn.Click += new System.EventHandler(this.randomOptionsDefaultBtn_Click);
			// 
			// label7
			// 
			this.label7.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.label7.Location = new System.Drawing.Point(16, 24);
			this.label7.Name = "label7";
			this.label7.Size = new System.Drawing.Size(40, 16);
			this.label7.TabIndex = 49;
			this.label7.Text = "Seed:";
			// 
			// label10
			// 
			this.label10.Location = new System.Drawing.Point(8, 120);
			this.label10.Name = "label10";
			this.label10.Size = new System.Drawing.Size(32, 16);
			this.label10.TabIndex = 48;
			this.label10.Text = "Max";
			this.label10.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			// 
			// label8
			// 
			this.label8.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.label8.Location = new System.Drawing.Point(16, 72);
			this.label8.Name = "label8";
			this.label8.Size = new System.Drawing.Size(48, 16);
			this.label8.TabIndex = 46;
			this.label8.Text = "Range:";
			// 
			// label9
			// 
			this.label9.Location = new System.Drawing.Point(16, 96);
			this.label9.Name = "label9";
			this.label9.Size = new System.Drawing.Size(24, 16);
			this.label9.TabIndex = 47;
			this.label9.Text = "Min";
			this.label9.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			// 
			// timeAsSeedCheckBox
			// 
			this.timeAsSeedCheckBox.Location = new System.Drawing.Point(136, 52);
			this.timeAsSeedCheckBox.Name = "timeAsSeedCheckBox";
			this.timeAsSeedCheckBox.RightToLeft = System.Windows.Forms.RightToLeft.No;
			this.timeAsSeedCheckBox.Size = new System.Drawing.Size(72, 16);
			this.timeAsSeedCheckBox.TabIndex = 3;
			this.timeAsSeedCheckBox.Text = "Use time";
			this.toolTip.SetToolTip(this.timeAsSeedCheckBox, "use time as radom seed");
			this.timeAsSeedCheckBox.CheckedChanged += new System.EventHandler(this.randomOptionsCheckBoxs_CheckedChanged);
			// 
			// optimalRangeCheckBox
			// 
			this.optimalRangeCheckBox.Location = new System.Drawing.Point(136, 112);
			this.optimalRangeCheckBox.Name = "optimalRangeCheckBox";
			this.optimalRangeCheckBox.Size = new System.Drawing.Size(88, 16);
			this.optimalRangeCheckBox.TabIndex = 6;
			this.optimalRangeCheckBox.Text = "Use optimal";
			this.toolTip.SetToolTip(this.optimalRangeCheckBox, "use optimal range for randomizing weights");
			this.optimalRangeCheckBox.CheckedChanged += new System.EventHandler(this.randomOptionsCheckBoxs_CheckedChanged);
			// 
			// rangeMaxUpDown
			// 
			this.rangeMaxUpDown.DecimalPlaces = 2;
			this.rangeMaxUpDown.Increment = new System.Decimal(new int[] {
																			 1,
																			 0,
																			 0,
																			 131072});
			this.rangeMaxUpDown.Location = new System.Drawing.Point(48, 120);
			this.rangeMaxUpDown.Minimum = new System.Decimal(new int[] {
																		   100,
																		   0,
																		   0,
																		   -2147483648});
			this.rangeMaxUpDown.Name = "rangeMaxUpDown";
			this.rangeMaxUpDown.Size = new System.Drawing.Size(80, 20);
			this.rangeMaxUpDown.TabIndex = 5;
			this.rangeMaxUpDown.ValueChanged += new System.EventHandler(this.rangeMaxUpDown_ValueChanged);
			// 
			// rangeMinUpDown
			// 
			this.rangeMinUpDown.DecimalPlaces = 2;
			this.rangeMinUpDown.Increment = new System.Decimal(new int[] {
																			 1,
																			 0,
																			 0,
																			 131072});
			this.rangeMinUpDown.Location = new System.Drawing.Point(48, 96);
			this.rangeMinUpDown.Minimum = new System.Decimal(new int[] {
																		   100,
																		   0,
																		   0,
																		   -2147483648});
			this.rangeMinUpDown.Name = "rangeMinUpDown";
			this.rangeMinUpDown.Size = new System.Drawing.Size(80, 20);
			this.rangeMinUpDown.TabIndex = 4;
			this.rangeMinUpDown.Value = new System.Decimal(new int[] {
																		 1,
																		 0,
																		 0,
																		 131072});
			this.rangeMinUpDown.ValueChanged += new System.EventHandler(this.rangeMinUpDown_ValueChanged);
			// 
			// seedUpDown
			// 
			this.seedUpDown.Location = new System.Drawing.Point(48, 48);
			this.seedUpDown.Maximum = new System.Decimal(new int[] {
																	   2147483647,
																	   0,
																	   0,
																	   0});
			this.seedUpDown.Minimum = new System.Decimal(new int[] {
																	   -2147483648,
																	   0,
																	   0,
																	   -2147483648});
			this.seedUpDown.Name = "seedUpDown";
			this.seedUpDown.Size = new System.Drawing.Size(80, 20);
			this.seedUpDown.TabIndex = 2;
			this.seedUpDown.ValueChanged += new System.EventHandler(this.seedUpDown_ValueChanged);
			// 
			// netNameTxt
			// 
			this.netNameTxt.AcceptsReturn = true;
			this.netNameTxt.Location = new System.Drawing.Point(16, 48);
			this.netNameTxt.Name = "netNameTxt";
			this.netNameTxt.Size = new System.Drawing.Size(232, 20);
			this.netNameTxt.TabIndex = 0;
			this.netNameTxt.Text = "";
			this.netNameTxt.Leave += new System.EventHandler(this.netNameTxt_Leave);
			this.netNameTxt.KeyUp += new System.Windows.Forms.KeyEventHandler(this.netNameTxt_KeyUp);
			// 
			// label4
			// 
			this.label4.Location = new System.Drawing.Point(16, 32);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(56, 16);
			this.label4.TabIndex = 16;
			this.label4.Text = "Net name:";
			this.label4.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// deleteLayerBtn
			// 
			this.deleteLayerBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.deleteLayerBtn.Location = new System.Drawing.Point(336, 560);
			this.deleteLayerBtn.Name = "deleteLayerBtn";
			this.deleteLayerBtn.Size = new System.Drawing.Size(120, 24);
			this.deleteLayerBtn.TabIndex = 11;
			this.deleteLayerBtn.Text = "Delete layer";
			this.deleteLayerBtn.Click += new System.EventHandler(this.deleteLayerBtn_Click);
			// 
			// netTreeCollapse_Btn
			// 
			this.netTreeCollapse_Btn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.netTreeCollapse_Btn.Location = new System.Drawing.Point(480, 456);
			this.netTreeCollapse_Btn.Name = "netTreeCollapse_Btn";
			this.netTreeCollapse_Btn.Size = new System.Drawing.Size(120, 24);
			this.netTreeCollapse_Btn.TabIndex = 10;
			this.netTreeCollapse_Btn.Text = "Collapse all nodes";
			this.netTreeCollapse_Btn.Click += new System.EventHandler(this.netTreeCollapse_Btn_Click);
			// 
			// netTreeExpand_Btn
			// 
			this.netTreeExpand_Btn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.netTreeExpand_Btn.Location = new System.Drawing.Point(336, 456);
			this.netTreeExpand_Btn.Name = "netTreeExpand_Btn";
			this.netTreeExpand_Btn.Size = new System.Drawing.Size(120, 24);
			this.netTreeExpand_Btn.TabIndex = 9;
			this.netTreeExpand_Btn.Text = "Expand all nodes";
			this.netTreeExpand_Btn.Click += new System.EventHandler(this.netTreeExpand_Btn_Click);
			// 
			// generateNetBtn
			// 
			this.generateNetBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.generateNetBtn.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.generateNetBtn.Location = new System.Drawing.Point(664, 560);
			this.generateNetBtn.Name = "generateNetBtn";
			this.generateNetBtn.Size = new System.Drawing.Size(120, 24);
			this.generateNetBtn.TabIndex = 17;
			this.generateNetBtn.Text = "Generate neural net";
			this.generateNetBtn.Click += new System.EventHandler(this.generateNetBtn_Click);
			// 
			// insertLayerBeforeBtn
			// 
			this.insertLayerBeforeBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.insertLayerBeforeBtn.Location = new System.Drawing.Point(480, 560);
			this.insertLayerBeforeBtn.Name = "insertLayerBeforeBtn";
			this.insertLayerBeforeBtn.Size = new System.Drawing.Size(120, 23);
			this.insertLayerBeforeBtn.TabIndex = 13;
			this.insertLayerBeforeBtn.Text = "Insert before";
			this.toolTip.SetToolTip(this.insertLayerBeforeBtn, "insert layer before the selected");
			this.insertLayerBeforeBtn.Click += new System.EventHandler(this.insertLayerBeforeBtn_Click);
			// 
			// neuronsNumTxt
			// 
			this.neuronsNumTxt.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.neuronsNumTxt.Location = new System.Drawing.Point(480, 496);
			this.neuronsNumTxt.Name = "neuronsNumTxt";
			this.neuronsNumTxt.Size = new System.Drawing.Size(120, 20);
			this.neuronsNumTxt.TabIndex = 12;
			this.neuronsNumTxt.Text = "";
			// 
			// clearBtn
			// 
			this.clearBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.clearBtn.Location = new System.Drawing.Point(664, 456);
			this.clearBtn.Name = "clearBtn";
			this.clearBtn.Size = new System.Drawing.Size(120, 24);
			this.clearBtn.TabIndex = 15;
			this.clearBtn.Text = "Clear view";
			this.clearBtn.Click += new System.EventHandler(this.clearBtn_Click);
			// 
			// showNetBtn
			// 
			this.showNetBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.showNetBtn.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.showNetBtn.Location = new System.Drawing.Point(808, 456);
			this.showNetBtn.Name = "showNetBtn";
			this.showNetBtn.Size = new System.Drawing.Size(120, 24);
			this.showNetBtn.TabIndex = 16;
			this.showNetBtn.Text = "Show current net";
			this.showNetBtn.Click += new System.EventHandler(this.showNetBtn_Click);
			// 
			// netTreeView
			// 
			this.netTreeView.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
				| System.Windows.Forms.AnchorStyles.Left)));
			this.netTreeView.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.netTreeView.HideSelection = false;
			this.netTreeView.ImageIndex = -1;
			this.netTreeView.Location = new System.Drawing.Point(336, 56);
			this.netTreeView.Name = "netTreeView";
			this.netTreeView.SelectedImageIndex = -1;
			this.netTreeView.Size = new System.Drawing.Size(264, 389);
			this.netTreeView.TabIndex = 8;
			// 
			// label3
			// 
			this.label3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.label3.Location = new System.Drawing.Point(336, 496);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(152, 16);
			this.label3.TabIndex = 15;
			this.label3.Text = "Number of neurons in layer:";
			this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// patternPage
			// 
			this.patternPage.Controls.Add(this.patternModeGroupBox);
			this.patternPage.Controls.Add(this.ocrModePanel);
			this.patternPage.Controls.Add(this.manualModePanel);
			this.patternPage.Location = new System.Drawing.Point(4, 22);
			this.patternPage.Name = "patternPage";
			this.patternPage.Size = new System.Drawing.Size(944, 590);
			this.patternPage.TabIndex = 2;
			this.patternPage.Text = "Pattern Builder";
			// 
			// patternModeGroupBox
			// 
			this.patternModeGroupBox.Controls.Add(this.manualModeRadioBtn);
			this.patternModeGroupBox.Controls.Add(this.ocrModeRadioBtn);
			this.patternModeGroupBox.Location = new System.Drawing.Point(16, 16);
			this.patternModeGroupBox.Name = "patternModeGroupBox";
			this.patternModeGroupBox.Size = new System.Drawing.Size(248, 40);
			this.patternModeGroupBox.TabIndex = 23;
			this.patternModeGroupBox.TabStop = false;
			this.patternModeGroupBox.Text = "Mode";
			this.toolTip.SetToolTip(this.patternModeGroupBox, "select the mode for pattern building");
			// 
			// manualModeRadioBtn
			// 
			this.manualModeRadioBtn.Location = new System.Drawing.Point(152, 16);
			this.manualModeRadioBtn.Name = "manualModeRadioBtn";
			this.manualModeRadioBtn.Size = new System.Drawing.Size(88, 16);
			this.manualModeRadioBtn.TabIndex = 20;
			this.manualModeRadioBtn.Text = "Manual input";
			this.manualModeRadioBtn.Click += new System.EventHandler(this.patternModeRadioBtn_CheckedChanged);
			this.manualModeRadioBtn.CheckedChanged += new System.EventHandler(this.patternModeRadioBtn_CheckedChanged);
			// 
			// ocrModeRadioBtn
			// 
			this.ocrModeRadioBtn.Checked = true;
			this.ocrModeRadioBtn.Location = new System.Drawing.Point(8, 16);
			this.ocrModeRadioBtn.Name = "ocrModeRadioBtn";
			this.ocrModeRadioBtn.Size = new System.Drawing.Size(128, 16);
			this.ocrModeRadioBtn.TabIndex = 19;
			this.ocrModeRadioBtn.TabStop = true;
			this.ocrModeRadioBtn.Text = "OCR pre-processing";
			this.ocrModeRadioBtn.Click += new System.EventHandler(this.patternModeRadioBtn_CheckedChanged);
			this.ocrModeRadioBtn.CheckedChanged += new System.EventHandler(this.patternModeRadioBtn_CheckedChanged);
			// 
			// manualModePanel
			// 
			this.manualModePanel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
				| System.Windows.Forms.AnchorStyles.Left) 
				| System.Windows.Forms.AnchorStyles.Right)));
			this.manualModePanel.Controls.Add(this.top10_GroupBox);
			this.manualModePanel.Controls.Add(this.patternTreeCollapse_Btn);
			this.manualModePanel.Controls.Add(this.patternTreeExpand_Btn);
			this.manualModePanel.Controls.Add(this.label5);
			this.manualModePanel.Controls.Add(this.label2);
			this.manualModePanel.Controls.Add(this.label1);
			this.manualModePanel.Controls.Add(this.changeOutputPatternBtn);
			this.manualModePanel.Controls.Add(this.patternTreeView);
			this.manualModePanel.Controls.Add(this.patternOutputListView);
			this.manualModePanel.Controls.Add(this.deletePatternBtn);
			this.manualModePanel.Controls.Add(this.newPatternBtn);
			this.manualModePanel.Controls.Add(this.clearPaternsBtn);
			this.manualModePanel.Controls.Add(this.currentPatternBtn);
			this.manualModePanel.Controls.Add(this.changeInputPatternBtn);
			this.manualModePanel.Controls.Add(this.outputTxt);
			this.manualModePanel.Controls.Add(this.inputTxt);
			this.manualModePanel.Controls.Add(this.patternInputListView);
			this.manualModePanel.Controls.Add(this.deleteAllPaternsBtn);
			this.manualModePanel.Location = new System.Drawing.Point(0, 64);
			this.manualModePanel.Name = "manualModePanel";
			this.manualModePanel.Size = new System.Drawing.Size(944, 520);
			this.manualModePanel.TabIndex = 21;
			// 
			// top10_GroupBox
			// 
			this.top10_GroupBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
				| System.Windows.Forms.AnchorStyles.Right)));
			this.top10_GroupBox.Controls.Add(this.top10_2_Text);
			this.top10_GroupBox.Controls.Add(this.top10_3_Text);
			this.top10_GroupBox.Controls.Add(this.top10_4_Text);
			this.top10_GroupBox.Controls.Add(this.top10_5_Text);
			this.top10_GroupBox.Controls.Add(this.top10_6_Text);
			this.top10_GroupBox.Controls.Add(this.top10_7_Text);
			this.top10_GroupBox.Controls.Add(this.top10_8_Text);
			this.top10_GroupBox.Controls.Add(this.top10_9_Text);
			this.top10_GroupBox.Controls.Add(this.top10_10_Text);
			this.top10_GroupBox.Controls.Add(this.top10_1_Text);
			this.top10_GroupBox.Controls.Add(this.top10_10_progBar);
			this.top10_GroupBox.Controls.Add(this.top10_1_progBar);
			this.top10_GroupBox.Controls.Add(this.assoziateNwLCB);
			this.top10_GroupBox.Controls.Add(this.top10_3_progBar);
			this.top10_GroupBox.Controls.Add(this.top10_9_progBar);
			this.top10_GroupBox.Controls.Add(this.top10_7_progBar);
			this.top10_GroupBox.Controls.Add(this.top10_2_progBar);
			this.top10_GroupBox.Controls.Add(this.top10_8_progBar);
			this.top10_GroupBox.Controls.Add(this.top10_6_progBar);
			this.top10_GroupBox.Controls.Add(this.top10_5_progBar);
			this.top10_GroupBox.Controls.Add(this.top10_4_progBar);
			this.top10_GroupBox.Location = new System.Drawing.Point(688, 24);
			this.top10_GroupBox.Name = "top10_GroupBox";
			this.top10_GroupBox.Size = new System.Drawing.Size(248, 408);
			this.top10_GroupBox.TabIndex = 47;
			this.top10_GroupBox.TabStop = false;
			this.top10_GroupBox.Text = "Top responding neurons";
			// 
			// top10_2_Text
			// 
			this.top10_2_Text.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.top10_2_Text.Location = new System.Drawing.Point(152, 64);
			this.top10_2_Text.Name = "top10_2_Text";
			this.top10_2_Text.Size = new System.Drawing.Size(88, 16);
			this.top10_2_Text.TabIndex = 55;
			// 
			// top10_3_Text
			// 
			this.top10_3_Text.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.top10_3_Text.Location = new System.Drawing.Point(152, 104);
			this.top10_3_Text.Name = "top10_3_Text";
			this.top10_3_Text.Size = new System.Drawing.Size(88, 16);
			this.top10_3_Text.TabIndex = 54;
			// 
			// top10_4_Text
			// 
			this.top10_4_Text.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.top10_4_Text.Location = new System.Drawing.Point(152, 136);
			this.top10_4_Text.Name = "top10_4_Text";
			this.top10_4_Text.Size = new System.Drawing.Size(88, 16);
			this.top10_4_Text.TabIndex = 53;
			// 
			// top10_5_Text
			// 
			this.top10_5_Text.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.top10_5_Text.Location = new System.Drawing.Point(152, 176);
			this.top10_5_Text.Name = "top10_5_Text";
			this.top10_5_Text.Size = new System.Drawing.Size(88, 16);
			this.top10_5_Text.TabIndex = 52;
			// 
			// top10_6_Text
			// 
			this.top10_6_Text.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.top10_6_Text.Location = new System.Drawing.Point(152, 208);
			this.top10_6_Text.Name = "top10_6_Text";
			this.top10_6_Text.Size = new System.Drawing.Size(88, 16);
			this.top10_6_Text.TabIndex = 51;
			// 
			// top10_7_Text
			// 
			this.top10_7_Text.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.top10_7_Text.Location = new System.Drawing.Point(152, 248);
			this.top10_7_Text.Name = "top10_7_Text";
			this.top10_7_Text.Size = new System.Drawing.Size(88, 16);
			this.top10_7_Text.TabIndex = 50;
			// 
			// top10_8_Text
			// 
			this.top10_8_Text.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.top10_8_Text.Location = new System.Drawing.Point(152, 280);
			this.top10_8_Text.Name = "top10_8_Text";
			this.top10_8_Text.Size = new System.Drawing.Size(88, 16);
			this.top10_8_Text.TabIndex = 49;
			// 
			// top10_9_Text
			// 
			this.top10_9_Text.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.top10_9_Text.Location = new System.Drawing.Point(152, 320);
			this.top10_9_Text.Name = "top10_9_Text";
			this.top10_9_Text.Size = new System.Drawing.Size(88, 16);
			this.top10_9_Text.TabIndex = 48;
			// 
			// top10_10_Text
			// 
			this.top10_10_Text.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.top10_10_Text.Location = new System.Drawing.Point(152, 352);
			this.top10_10_Text.Name = "top10_10_Text";
			this.top10_10_Text.Size = new System.Drawing.Size(88, 16);
			this.top10_10_Text.TabIndex = 47;
			// 
			// top10_1_Text
			// 
			this.top10_1_Text.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.top10_1_Text.Location = new System.Drawing.Point(152, 32);
			this.top10_1_Text.Name = "top10_1_Text";
			this.top10_1_Text.Size = new System.Drawing.Size(88, 16);
			this.top10_1_Text.TabIndex = 46;
			// 
			// top10_10_progBar
			// 
			this.top10_10_progBar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
				| System.Windows.Forms.AnchorStyles.Right)));
			this.top10_10_progBar.Location = new System.Drawing.Point(16, 352);
			this.top10_10_progBar.Name = "top10_10_progBar";
			this.top10_10_progBar.Size = new System.Drawing.Size(128, 16);
			this.top10_10_progBar.TabIndex = 44;
			// 
			// top10_1_progBar
			// 
			this.top10_1_progBar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
				| System.Windows.Forms.AnchorStyles.Right)));
			this.top10_1_progBar.Location = new System.Drawing.Point(16, 32);
			this.top10_1_progBar.Name = "top10_1_progBar";
			this.top10_1_progBar.Size = new System.Drawing.Size(128, 16);
			this.top10_1_progBar.TabIndex = 35;
			// 
			// assoziateNwLCB
			// 
			this.assoziateNwLCB.Location = new System.Drawing.Point(16, 376);
			this.assoziateNwLCB.Name = "assoziateNwLCB";
			this.assoziateNwLCB.Size = new System.Drawing.Size(176, 24);
			this.assoziateNwLCB.TabIndex = 75;
			this.assoziateNwLCB.Text = "Associate neurons with letters";
			this.assoziateNwLCB.CheckedChanged += new System.EventHandler(this.assoziateNwLCB_CheckedChanged);
			// 
			// top10_3_progBar
			// 
			this.top10_3_progBar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
				| System.Windows.Forms.AnchorStyles.Right)));
			this.top10_3_progBar.Location = new System.Drawing.Point(16, 104);
			this.top10_3_progBar.Name = "top10_3_progBar";
			this.top10_3_progBar.Size = new System.Drawing.Size(128, 16);
			this.top10_3_progBar.TabIndex = 37;
			// 
			// top10_9_progBar
			// 
			this.top10_9_progBar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
				| System.Windows.Forms.AnchorStyles.Right)));
			this.top10_9_progBar.Location = new System.Drawing.Point(16, 320);
			this.top10_9_progBar.Name = "top10_9_progBar";
			this.top10_9_progBar.Size = new System.Drawing.Size(128, 16);
			this.top10_9_progBar.TabIndex = 43;
			// 
			// top10_7_progBar
			// 
			this.top10_7_progBar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
				| System.Windows.Forms.AnchorStyles.Right)));
			this.top10_7_progBar.Location = new System.Drawing.Point(16, 248);
			this.top10_7_progBar.Name = "top10_7_progBar";
			this.top10_7_progBar.Size = new System.Drawing.Size(128, 16);
			this.top10_7_progBar.TabIndex = 41;
			// 
			// top10_2_progBar
			// 
			this.top10_2_progBar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
				| System.Windows.Forms.AnchorStyles.Right)));
			this.top10_2_progBar.Location = new System.Drawing.Point(16, 64);
			this.top10_2_progBar.Name = "top10_2_progBar";
			this.top10_2_progBar.Size = new System.Drawing.Size(128, 16);
			this.top10_2_progBar.TabIndex = 36;
			// 
			// top10_8_progBar
			// 
			this.top10_8_progBar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
				| System.Windows.Forms.AnchorStyles.Right)));
			this.top10_8_progBar.Location = new System.Drawing.Point(16, 280);
			this.top10_8_progBar.Name = "top10_8_progBar";
			this.top10_8_progBar.Size = new System.Drawing.Size(128, 16);
			this.top10_8_progBar.TabIndex = 42;
			// 
			// top10_6_progBar
			// 
			this.top10_6_progBar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
				| System.Windows.Forms.AnchorStyles.Right)));
			this.top10_6_progBar.Location = new System.Drawing.Point(16, 208);
			this.top10_6_progBar.Name = "top10_6_progBar";
			this.top10_6_progBar.Size = new System.Drawing.Size(128, 16);
			this.top10_6_progBar.TabIndex = 40;
			// 
			// top10_5_progBar
			// 
			this.top10_5_progBar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
				| System.Windows.Forms.AnchorStyles.Right)));
			this.top10_5_progBar.Location = new System.Drawing.Point(16, 176);
			this.top10_5_progBar.Name = "top10_5_progBar";
			this.top10_5_progBar.Size = new System.Drawing.Size(128, 16);
			this.top10_5_progBar.TabIndex = 39;
			// 
			// top10_4_progBar
			// 
			this.top10_4_progBar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
				| System.Windows.Forms.AnchorStyles.Right)));
			this.top10_4_progBar.Location = new System.Drawing.Point(16, 136);
			this.top10_4_progBar.Name = "top10_4_progBar";
			this.top10_4_progBar.Size = new System.Drawing.Size(128, 16);
			this.top10_4_progBar.TabIndex = 38;
			// 
			// patternTreeCollapse_Btn
			// 
			this.patternTreeCollapse_Btn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.patternTreeCollapse_Btn.Location = new System.Drawing.Point(184, 376);
			this.patternTreeCollapse_Btn.Name = "patternTreeCollapse_Btn";
			this.patternTreeCollapse_Btn.Size = new System.Drawing.Size(136, 23);
			this.patternTreeCollapse_Btn.TabIndex = 67;
			this.patternTreeCollapse_Btn.Text = "Collapse all nodes";
			this.patternTreeCollapse_Btn.Click += new System.EventHandler(this.patternTreeCollapse_Btn_Click);
			// 
			// patternTreeExpand_Btn
			// 
			this.patternTreeExpand_Btn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.patternTreeExpand_Btn.Location = new System.Drawing.Point(184, 344);
			this.patternTreeExpand_Btn.Name = "patternTreeExpand_Btn";
			this.patternTreeExpand_Btn.Size = new System.Drawing.Size(136, 23);
			this.patternTreeExpand_Btn.TabIndex = 66;
			this.patternTreeExpand_Btn.Text = "Expand all nodes";
			this.patternTreeExpand_Btn.Click += new System.EventHandler(this.patternTreeExpand_Btn_Click);
			// 
			// label5
			// 
			this.label5.Location = new System.Drawing.Point(16, 8);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(100, 16);
			this.label5.TabIndex = 32;
			this.label5.Text = "Pattern overview";
			// 
			// label2
			// 
			this.label2.Location = new System.Drawing.Point(496, 8);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(80, 16);
			this.label2.TabIndex = 30;
			this.label2.Text = "Output";
			// 
			// label1
			// 
			this.label1.Location = new System.Drawing.Point(352, 8);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(32, 16);
			this.label1.TabIndex = 29;
			this.label1.Text = "Input:";
			// 
			// changeOutputPatternBtn
			// 
			this.changeOutputPatternBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.changeOutputPatternBtn.Location = new System.Drawing.Point(496, 488);
			this.changeOutputPatternBtn.Name = "changeOutputPatternBtn";
			this.changeOutputPatternBtn.Size = new System.Drawing.Size(176, 24);
			this.changeOutputPatternBtn.TabIndex = 74;
			this.changeOutputPatternBtn.Text = "Change";
			this.toolTip.SetToolTip(this.changeOutputPatternBtn, "change the selected value");
			this.changeOutputPatternBtn.Click += new System.EventHandler(this.changeOutputPatternBtn_Click);
			// 
			// patternTreeView
			// 
			this.patternTreeView.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
				| System.Windows.Forms.AnchorStyles.Left)));
			this.patternTreeView.HideSelection = false;
			this.patternTreeView.ImageIndex = -1;
			this.patternTreeView.Location = new System.Drawing.Point(16, 24);
			this.patternTreeView.Name = "patternTreeView";
			this.patternTreeView.SelectedImageIndex = -1;
			this.patternTreeView.Size = new System.Drawing.Size(160, 452);
			this.patternTreeView.TabIndex = 61;
			this.patternTreeView.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.patternTreeView_AfterSelect);
			// 
			// patternOutputListView
			// 
			this.patternOutputListView.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
				| System.Windows.Forms.AnchorStyles.Left)));
			this.patternOutputListView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
																									this.OutputNeuron,
																									this.OutputValue,
																									this.realOutValue});
			this.patternOutputListView.FullRowSelect = true;
			this.patternOutputListView.HideSelection = false;
			this.patternOutputListView.ImeMode = System.Windows.Forms.ImeMode.NoControl;
			this.patternOutputListView.Location = new System.Drawing.Point(496, 24);
			this.patternOutputListView.MultiSelect = false;
			this.patternOutputListView.Name = "patternOutputListView";
			this.patternOutputListView.Size = new System.Drawing.Size(176, 429);
			this.patternOutputListView.TabIndex = 72;
			this.patternOutputListView.View = System.Windows.Forms.View.Details;
			this.patternOutputListView.SelectedIndexChanged += new System.EventHandler(this.patternOutputListView_SelectedIndexChanged);
			// 
			// OutputNeuron
			// 
			this.OutputNeuron.Text = "Neuron";
			this.OutputNeuron.Width = 49;
			// 
			// OutputValue
			// 
			this.OutputValue.Text = "Teach";
			this.OutputValue.Width = 44;
			// 
			// realOutValue
			// 
			this.realOutValue.Text = "Real";
			this.realOutValue.Width = 79;
			// 
			// deletePatternBtn
			// 
			this.deletePatternBtn.Location = new System.Drawing.Point(184, 136);
			this.deletePatternBtn.Name = "deletePatternBtn";
			this.deletePatternBtn.Size = new System.Drawing.Size(136, 23);
			this.deletePatternBtn.TabIndex = 65;
			this.deletePatternBtn.Text = "Delete selected pattern";
			this.deletePatternBtn.Click += new System.EventHandler(this.deletePatternBtn_Click);
			// 
			// newPatternBtn
			// 
			this.newPatternBtn.Location = new System.Drawing.Point(184, 104);
			this.newPatternBtn.Name = "newPatternBtn";
			this.newPatternBtn.Size = new System.Drawing.Size(136, 23);
			this.newPatternBtn.TabIndex = 64;
			this.newPatternBtn.Text = "Create new pattern";
			this.newPatternBtn.Click += new System.EventHandler(this.newPatternBtn_Click);
			// 
			// clearPaternsBtn
			// 
			this.clearPaternsBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.clearPaternsBtn.Location = new System.Drawing.Point(184, 453);
			this.clearPaternsBtn.Name = "clearPaternsBtn";
			this.clearPaternsBtn.Size = new System.Drawing.Size(136, 23);
			this.clearPaternsBtn.TabIndex = 68;
			this.clearPaternsBtn.Text = "Clear view";
			this.clearPaternsBtn.Click += new System.EventHandler(this.clearPaternsBtn_Click);
			// 
			// currentPatternBtn
			// 
			this.currentPatternBtn.Location = new System.Drawing.Point(184, 24);
			this.currentPatternBtn.Name = "currentPatternBtn";
			this.currentPatternBtn.Size = new System.Drawing.Size(136, 23);
			this.currentPatternBtn.TabIndex = 62;
			this.currentPatternBtn.Text = "Show current patterns";
			this.currentPatternBtn.Click += new System.EventHandler(this.currentPatternBtn_Click);
			// 
			// changeInputPatternBtn
			// 
			this.changeInputPatternBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.changeInputPatternBtn.Location = new System.Drawing.Point(352, 488);
			this.changeInputPatternBtn.Name = "changeInputPatternBtn";
			this.changeInputPatternBtn.Size = new System.Drawing.Size(136, 24);
			this.changeInputPatternBtn.TabIndex = 71;
			this.changeInputPatternBtn.Text = "Change";
			this.toolTip.SetToolTip(this.changeInputPatternBtn, "change the selected value");
			this.changeInputPatternBtn.Click += new System.EventHandler(this.changeInputPatternBtn_Click);
			// 
			// outputTxt
			// 
			this.outputTxt.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.outputTxt.Location = new System.Drawing.Point(496, 456);
			this.outputTxt.Name = "outputTxt";
			this.outputTxt.Size = new System.Drawing.Size(176, 20);
			this.outputTxt.TabIndex = 73;
			this.outputTxt.Text = "";
			this.outputTxt.KeyUp += new System.Windows.Forms.KeyEventHandler(this.manualPatternModeTextBox_KeyUp);
			// 
			// inputTxt
			// 
			this.inputTxt.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.inputTxt.Location = new System.Drawing.Point(352, 456);
			this.inputTxt.Name = "inputTxt";
			this.inputTxt.Size = new System.Drawing.Size(136, 20);
			this.inputTxt.TabIndex = 70;
			this.inputTxt.Text = "";
			this.inputTxt.KeyUp += new System.Windows.Forms.KeyEventHandler(this.manualPatternModeTextBox_KeyUp);
			// 
			// patternInputListView
			// 
			this.patternInputListView.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
				| System.Windows.Forms.AnchorStyles.Left)));
			this.patternInputListView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
																								   this.InputNeuron,
																								   this.InputValue});
			this.patternInputListView.FullRowSelect = true;
			this.patternInputListView.HideSelection = false;
			this.patternInputListView.ImeMode = System.Windows.Forms.ImeMode.NoControl;
			this.patternInputListView.Location = new System.Drawing.Point(352, 24);
			this.patternInputListView.MultiSelect = false;
			this.patternInputListView.Name = "patternInputListView";
			this.patternInputListView.Size = new System.Drawing.Size(136, 429);
			this.patternInputListView.TabIndex = 69;
			this.patternInputListView.View = System.Windows.Forms.View.Details;
			this.patternInputListView.SelectedIndexChanged += new System.EventHandler(this.patternInputListView_SelectedIndexChanged);
			// 
			// InputNeuron
			// 
			this.InputNeuron.Text = "Neuron";
			this.InputNeuron.Width = 50;
			// 
			// InputValue
			// 
			this.InputValue.Text = "Value";
			this.InputValue.Width = 78;
			// 
			// deleteAllPaternsBtn
			// 
			this.deleteAllPaternsBtn.BackColor = System.Drawing.SystemColors.Control;
			this.deleteAllPaternsBtn.ForeColor = System.Drawing.SystemColors.ControlText;
			this.deleteAllPaternsBtn.Location = new System.Drawing.Point(184, 56);
			this.deleteAllPaternsBtn.Name = "deleteAllPaternsBtn";
			this.deleteAllPaternsBtn.Size = new System.Drawing.Size(136, 23);
			this.deleteAllPaternsBtn.TabIndex = 63;
			this.deleteAllPaternsBtn.Text = "Delete current patterns";
			this.deleteAllPaternsBtn.Click += new System.EventHandler(this.deleteAllPaternsBtn_Click);
			// 
			// ocrModePanel
			// 
			this.ocrModePanel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
				| System.Windows.Forms.AnchorStyles.Left) 
				| System.Windows.Forms.AnchorStyles.Right)));
			this.ocrModePanel.Controls.Add(this.imgSepGroupBox);
			this.ocrModePanel.Controls.Add(this.startExtractBtn);
			this.ocrModePanel.Controls.Add(this.stopExtractBtn);
			this.ocrModePanel.Controls.Add(this.sepImgGroupBox);
			this.ocrModePanel.Controls.Add(this.creationRadioBtn);
			this.ocrModePanel.Controls.Add(this.groupBox5);
			this.ocrModePanel.Controls.Add(this.featureExtractGroupBox);
			this.ocrModePanel.Controls.Add(this.groupBox3);
			this.ocrModePanel.Controls.Add(this.filterRadioBtn);
			this.ocrModePanel.Controls.Add(this.startGeneratePatternsBtn);
			this.ocrModePanel.Controls.Add(this.stopGeneratePatternsBtn);
			this.ocrModePanel.Location = new System.Drawing.Point(0, 56);
			this.ocrModePanel.Name = "ocrModePanel";
			this.ocrModePanel.Size = new System.Drawing.Size(944, 536);
			this.ocrModePanel.TabIndex = 20;
			this.ocrModePanel.Paint += new System.Windows.Forms.PaintEventHandler(this.ocrModePanel_Paint);
			// 
			// imgSepGroupBox
			// 
			this.imgSepGroupBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.imgSepGroupBox.Controls.Add(this.imgSepThreshHoriznUpDown);
			this.imgSepGroupBox.Controls.Add(this.imgSepThreshVertnUpDown);
			this.imgSepGroupBox.Controls.Add(this.threshVertLab);
			this.imgSepGroupBox.Controls.Add(this.threshHorizLab);
			this.imgSepGroupBox.Location = new System.Drawing.Point(208, 384);
			this.imgSepGroupBox.Name = "imgSepGroupBox";
			this.imgSepGroupBox.Size = new System.Drawing.Size(288, 40);
			this.imgSepGroupBox.TabIndex = 44;
			this.imgSepGroupBox.TabStop = false;
			this.imgSepGroupBox.Text = "7. Character separator histogram properties";
			// 
			// imgSepThreshHoriznUpDown
			// 
			this.imgSepThreshHoriznUpDown.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.imgSepThreshHoriznUpDown.DecimalPlaces = 3;
			this.imgSepThreshHoriznUpDown.Increment = new System.Decimal(new int[] {
																					   1,
																					   0,
																					   0,
																					   196608});
			this.imgSepThreshHoriznUpDown.Location = new System.Drawing.Point(230, 16);
			this.imgSepThreshHoriznUpDown.Maximum = new System.Decimal(new int[] {
																					 1,
																					 0,
																					 0,
																					 0});
			this.imgSepThreshHoriznUpDown.Name = "imgSepThreshHoriznUpDown";
			this.imgSepThreshHoriznUpDown.Size = new System.Drawing.Size(50, 20);
			this.imgSepThreshHoriznUpDown.TabIndex = 36;
			// 
			// imgSepThreshVertnUpDown
			// 
			this.imgSepThreshVertnUpDown.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.imgSepThreshVertnUpDown.DecimalPlaces = 3;
			this.imgSepThreshVertnUpDown.Increment = new System.Decimal(new int[] {
																					  1,
																					  0,
																					  0,
																					  196608});
			this.imgSepThreshVertnUpDown.Location = new System.Drawing.Point(88, 16);
			this.imgSepThreshVertnUpDown.Maximum = new System.Decimal(new int[] {
																					1,
																					0,
																					0,
																					0});
			this.imgSepThreshVertnUpDown.Name = "imgSepThreshVertnUpDown";
			this.imgSepThreshVertnUpDown.Size = new System.Drawing.Size(48, 20);
			this.imgSepThreshVertnUpDown.TabIndex = 35;
			// 
			// threshVertLab
			// 
			this.threshVertLab.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.threshVertLab.Location = new System.Drawing.Point(8, 20);
			this.threshVertLab.Name = "threshVertLab";
			this.threshVertLab.Size = new System.Drawing.Size(88, 16);
			this.threshVertLab.TabIndex = 32;
			this.threshVertLab.Text = "Threshold col.:";
			// 
			// threshHorizLab
			// 
			this.threshHorizLab.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.threshHorizLab.Location = new System.Drawing.Point(147, 20);
			this.threshHorizLab.Name = "threshHorizLab";
			this.threshHorizLab.Size = new System.Drawing.Size(96, 16);
			this.threshHorizLab.TabIndex = 34;
			this.threshHorizLab.Text = "Threshold row:";
			// 
			// startExtractBtn
			// 
			this.startExtractBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.startExtractBtn.BackColor = System.Drawing.SystemColors.Control;
			this.startExtractBtn.Enabled = false;
			this.startExtractBtn.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.startExtractBtn.ForeColor = System.Drawing.SystemColors.ControlText;
			this.startExtractBtn.Location = new System.Drawing.Point(688, 240);
			this.startExtractBtn.Name = "startExtractBtn";
			this.startExtractBtn.Size = new System.Drawing.Size(40, 24);
			this.startExtractBtn.TabIndex = 59;
			this.startExtractBtn.Text = "Start";
			this.toolTip.SetToolTip(this.startExtractBtn, "Start creation of OCR training images or if \'Image filtering\' start filtering.\\nA" +
				"fter that extract the feature vector.");
			this.startExtractBtn.Click += new System.EventHandler(this.startExtractBtn_Click);
			// 
			// stopExtractBtn
			// 
			this.stopExtractBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.stopExtractBtn.Enabled = false;
			this.stopExtractBtn.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.stopExtractBtn.Location = new System.Drawing.Point(688, 296);
			this.stopExtractBtn.Name = "stopExtractBtn";
			this.stopExtractBtn.Size = new System.Drawing.Size(40, 24);
			this.stopExtractBtn.TabIndex = 60;
			this.stopExtractBtn.Text = "Abort";
			this.toolTip.SetToolTip(this.stopExtractBtn, "Abort/cancel filtering, creation.");
			this.stopExtractBtn.Click += new System.EventHandler(this.stopFilterBtn_Click);
			// 
			// sepImgGroupBox
			// 
			this.sepImgGroupBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
				| System.Windows.Forms.AnchorStyles.Right)));
			this.sepImgGroupBox.Controls.Add(this.addPatsCheckBox);
			this.sepImgGroupBox.Controls.Add(this.delImgBtn);
			this.sepImgGroupBox.Controls.Add(this.charsBmpUpDown);
			this.sepImgGroupBox.Controls.Add(this.picNumbLab);
			this.sepImgGroupBox.Controls.Add(this.charPB);
			this.sepImgGroupBox.Controls.Add(this.windowgroupBox);
			this.sepImgGroupBox.Controls.Add(this.scaleGroupBox);
			this.sepImgGroupBox.Controls.Add(this.generateNetcheckBox);
			this.sepImgGroupBox.Location = new System.Drawing.Point(544, 16);
			this.sepImgGroupBox.Name = "sepImgGroupBox";
			this.sepImgGroupBox.Size = new System.Drawing.Size(144, 496);
			this.sepImgGroupBox.TabIndex = 41;
			this.sepImgGroupBox.TabStop = false;
			this.sepImgGroupBox.Text = "Chararcter images";
			// 
			// addPatsCheckBox
			// 
			this.addPatsCheckBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.addPatsCheckBox.Enabled = false;
			this.addPatsCheckBox.Location = new System.Drawing.Point(16, 440);
			this.addPatsCheckBox.Name = "addPatsCheckBox";
			this.addPatsCheckBox.Size = new System.Drawing.Size(120, 16);
			this.addPatsCheckBox.TabIndex = 55;
			this.addPatsCheckBox.Text = "Add to old patterns";
			// 
			// delImgBtn
			// 
			this.delImgBtn.Enabled = false;
			this.delImgBtn.Location = new System.Drawing.Point(16, 176);
			this.delImgBtn.Name = "delImgBtn";
			this.delImgBtn.Size = new System.Drawing.Size(112, 23);
			this.delImgBtn.TabIndex = 47;
			this.delImgBtn.Text = "Delete image";
			this.delImgBtn.Click += new System.EventHandler(this.delImgBtn_Click);
			// 
			// charsBmpUpDown
			// 
			this.charsBmpUpDown.Location = new System.Drawing.Point(64, 144);
			this.charsBmpUpDown.Name = "charsBmpUpDown";
			this.charsBmpUpDown.ReadOnly = true;
			this.charsBmpUpDown.Size = new System.Drawing.Size(64, 20);
			this.charsBmpUpDown.TabIndex = 46;
			this.charsBmpUpDown.SelectedItemChanged += new System.EventHandler(this.charsBmpUpDown_SelectedItemChanged);
			// 
			// picNumbLab
			// 
			this.picNumbLab.Location = new System.Drawing.Point(16, 148);
			this.picNumbLab.Name = "picNumbLab";
			this.picNumbLab.Size = new System.Drawing.Size(49, 16);
			this.picNumbLab.TabIndex = 37;
			this.picNumbLab.Text = "Pic No.:";
			// 
			// charPB
			// 
			this.charPB.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.charPB.Location = new System.Drawing.Point(16, 24);
			this.charPB.Name = "charPB";
			this.charPB.Size = new System.Drawing.Size(112, 108);
			this.charPB.TabIndex = 34;
			this.charPB.TabStop = false;
			this.charPB.Paint += new System.Windows.Forms.PaintEventHandler(this.charPB_Paint);
			// 
			// windowgroupBox
			// 
			this.windowgroupBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.windowgroupBox.Controls.Add(this.heightnUpDown);
			this.windowgroupBox.Controls.Add(this.heightLab);
			this.windowgroupBox.Controls.Add(this.widthnUpDown);
			this.windowgroupBox.Controls.Add(this.widthLab);
			this.windowgroupBox.Controls.Add(this.ynUpDown);
			this.windowgroupBox.Controls.Add(this.yLab);
			this.windowgroupBox.Controls.Add(this.xnUpDown);
			this.windowgroupBox.Controls.Add(this.xLab);
			this.windowgroupBox.Location = new System.Drawing.Point(16, 224);
			this.windowgroupBox.Name = "windowgroupBox";
			this.windowgroupBox.Size = new System.Drawing.Size(112, 128);
			this.windowgroupBox.TabIndex = 39;
			this.windowgroupBox.TabStop = false;
			this.windowgroupBox.Text = "Extraction window";
			this.toolTip.SetToolTip(this.windowgroupBox, "choose the rectangle which will be evaluated by the extraction algorithm");
			// 
			// heightnUpDown
			// 
			this.heightnUpDown.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.heightnUpDown.Location = new System.Drawing.Point(56, 96);
			this.heightnUpDown.Maximum = new System.Decimal(new int[] {
																		  100000,
																		  0,
																		  0,
																		  0});
			this.heightnUpDown.Minimum = new System.Decimal(new int[] {
																		  1,
																		  0,
																		  0,
																		  0});
			this.heightnUpDown.Name = "heightnUpDown";
			this.heightnUpDown.Size = new System.Drawing.Size(48, 20);
			this.heightnUpDown.TabIndex = 51;
			this.heightnUpDown.Value = new System.Decimal(new int[] {
																		1,
																		0,
																		0,
																		0});
			this.heightnUpDown.ValueChanged += new System.EventHandler(this.extractWin_ValueChanged);
			// 
			// heightLab
			// 
			this.heightLab.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.heightLab.Location = new System.Drawing.Point(8, 100);
			this.heightLab.Name = "heightLab";
			this.heightLab.Size = new System.Drawing.Size(40, 16);
			this.heightLab.TabIndex = 6;
			this.heightLab.Text = "Height:";
			this.heightLab.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// widthnUpDown
			// 
			this.widthnUpDown.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.widthnUpDown.Location = new System.Drawing.Point(56, 72);
			this.widthnUpDown.Maximum = new System.Decimal(new int[] {
																		 100000,
																		 0,
																		 0,
																		 0});
			this.widthnUpDown.Minimum = new System.Decimal(new int[] {
																		 1,
																		 0,
																		 0,
																		 0});
			this.widthnUpDown.Name = "widthnUpDown";
			this.widthnUpDown.Size = new System.Drawing.Size(48, 20);
			this.widthnUpDown.TabIndex = 50;
			this.widthnUpDown.Value = new System.Decimal(new int[] {
																	   1,
																	   0,
																	   0,
																	   0});
			this.widthnUpDown.ValueChanged += new System.EventHandler(this.extractWin_ValueChanged);
			// 
			// widthLab
			// 
			this.widthLab.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.widthLab.Location = new System.Drawing.Point(8, 76);
			this.widthLab.Name = "widthLab";
			this.widthLab.Size = new System.Drawing.Size(40, 16);
			this.widthLab.TabIndex = 4;
			this.widthLab.Text = "Width:";
			this.widthLab.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// ynUpDown
			// 
			this.ynUpDown.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.ynUpDown.Location = new System.Drawing.Point(56, 48);
			this.ynUpDown.Maximum = new System.Decimal(new int[] {
																	 100000,
																	 0,
																	 0,
																	 0});
			this.ynUpDown.Name = "ynUpDown";
			this.ynUpDown.Size = new System.Drawing.Size(48, 20);
			this.ynUpDown.TabIndex = 49;
			this.ynUpDown.ValueChanged += new System.EventHandler(this.extractWin_ValueChanged);
			// 
			// yLab
			// 
			this.yLab.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.yLab.Location = new System.Drawing.Point(8, 52);
			this.yLab.Name = "yLab";
			this.yLab.Size = new System.Drawing.Size(40, 16);
			this.yLab.TabIndex = 2;
			this.yLab.Text = "y:";
			this.yLab.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// xnUpDown
			// 
			this.xnUpDown.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.xnUpDown.Location = new System.Drawing.Point(56, 24);
			this.xnUpDown.Maximum = new System.Decimal(new int[] {
																	 100000,
																	 0,
																	 0,
																	 0});
			this.xnUpDown.Name = "xnUpDown";
			this.xnUpDown.Size = new System.Drawing.Size(48, 20);
			this.xnUpDown.TabIndex = 48;
			this.xnUpDown.ValueChanged += new System.EventHandler(this.extractWin_ValueChanged);
			// 
			// xLab
			// 
			this.xLab.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.xLab.Location = new System.Drawing.Point(8, 28);
			this.xLab.Name = "xLab";
			this.xLab.Size = new System.Drawing.Size(40, 16);
			this.xLab.TabIndex = 0;
			this.xLab.Text = "x:";
			this.xLab.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// scaleGroupBox
			// 
			this.scaleGroupBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.scaleGroupBox.Controls.Add(this.heightScalenUpDown);
			this.scaleGroupBox.Controls.Add(this.heightScaleLab);
			this.scaleGroupBox.Controls.Add(this.widthScalenUpDown);
			this.scaleGroupBox.Controls.Add(this.widthScaleLab);
			this.scaleGroupBox.Location = new System.Drawing.Point(16, 352);
			this.scaleGroupBox.Name = "scaleGroupBox";
			this.scaleGroupBox.Size = new System.Drawing.Size(112, 80);
			this.scaleGroupBox.TabIndex = 40;
			this.scaleGroupBox.TabStop = false;
			this.scaleGroupBox.Text = "Scale images to:";
			this.toolTip.SetToolTip(this.scaleGroupBox, "choose the rectangle which will be evaluated by the extraction algorithm");
			// 
			// heightScalenUpDown
			// 
			this.heightScalenUpDown.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.heightScalenUpDown.Location = new System.Drawing.Point(56, 48);
			this.heightScalenUpDown.Maximum = new System.Decimal(new int[] {
																			   100000,
																			   0,
																			   0,
																			   0});
			this.heightScalenUpDown.Minimum = new System.Decimal(new int[] {
																			   1,
																			   0,
																			   0,
																			   0});
			this.heightScalenUpDown.Name = "heightScalenUpDown";
			this.heightScalenUpDown.Size = new System.Drawing.Size(48, 20);
			this.heightScalenUpDown.TabIndex = 53;
			this.heightScalenUpDown.Value = new System.Decimal(new int[] {
																			 1,
																			 0,
																			 0,
																			 0});
			// 
			// heightScaleLab
			// 
			this.heightScaleLab.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.heightScaleLab.Location = new System.Drawing.Point(8, 52);
			this.heightScaleLab.Name = "heightScaleLab";
			this.heightScaleLab.Size = new System.Drawing.Size(40, 16);
			this.heightScaleLab.TabIndex = 6;
			this.heightScaleLab.Text = "Height:";
			this.heightScaleLab.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// widthScalenUpDown
			// 
			this.widthScalenUpDown.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.widthScalenUpDown.Location = new System.Drawing.Point(56, 24);
			this.widthScalenUpDown.Maximum = new System.Decimal(new int[] {
																			  100000,
																			  0,
																			  0,
																			  0});
			this.widthScalenUpDown.Minimum = new System.Decimal(new int[] {
																			  1,
																			  0,
																			  0,
																			  0});
			this.widthScalenUpDown.Name = "widthScalenUpDown";
			this.widthScalenUpDown.Size = new System.Drawing.Size(48, 20);
			this.widthScalenUpDown.TabIndex = 52;
			this.widthScalenUpDown.Value = new System.Decimal(new int[] {
																			1,
																			0,
																			0,
																			0});
			// 
			// widthScaleLab
			// 
			this.widthScaleLab.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.widthScaleLab.Location = new System.Drawing.Point(8, 28);
			this.widthScaleLab.Name = "widthScaleLab";
			this.widthScaleLab.Size = new System.Drawing.Size(40, 16);
			this.widthScaleLab.TabIndex = 4;
			this.widthScaleLab.Text = "Width:";
			this.widthScaleLab.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// generateNetcheckBox
			// 
			this.generateNetcheckBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.generateNetcheckBox.Checked = true;
			this.generateNetcheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
			this.generateNetcheckBox.Location = new System.Drawing.Point(16, 456);
			this.generateNetcheckBox.Name = "generateNetcheckBox";
			this.generateNetcheckBox.Size = new System.Drawing.Size(120, 32);
			this.generateNetcheckBox.TabIndex = 54;
			this.generateNetcheckBox.Text = "Also generate corresponding net";
			this.generateNetcheckBox.CheckedChanged += new System.EventHandler(this.generateNetcheckBox_CheckedChanged);
			// 
			// creationRadioBtn
			// 
			this.creationRadioBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.creationRadioBtn.Checked = true;
			this.creationRadioBtn.Location = new System.Drawing.Point(496, 452);
			this.creationRadioBtn.Name = "creationRadioBtn";
			this.creationRadioBtn.Size = new System.Drawing.Size(16, 16);
			this.creationRadioBtn.TabIndex = 43;
			this.creationRadioBtn.TabStop = true;
			this.creationRadioBtn.CheckedChanged += new System.EventHandler(this.creationRadioBtn_CheckedChanged);
			// 
			// groupBox5
			// 
			this.groupBox5.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
				| System.Windows.Forms.AnchorStyles.Left) 
				| System.Windows.Forms.AnchorStyles.Right)));
			this.groupBox5.Controls.Add(this.onGrayCheckBox);
			this.groupBox5.Controls.Add(this.srcImgLab);
			this.groupBox5.Controls.Add(this.srcImgPB);
			this.groupBox5.Controls.Add(this.filteredImgPB);
			this.groupBox5.Controls.Add(this.filteredImgLab);
			this.groupBox5.Controls.Add(this.cannyGroupBox);
			this.groupBox5.Controls.Add(this.onBrightnCheckBox);
			this.groupBox5.Controls.Add(this.onHistCheckBox);
			this.groupBox5.Controls.Add(this.onBinCheckBox);
			this.groupBox5.Controls.Add(this.binThreshLab);
			this.groupBox5.Controls.Add(this.binUpDown);
			this.groupBox5.Controls.Add(this.segmRgnGroupBox);
			this.groupBox5.Location = new System.Drawing.Point(16, 16);
			this.groupBox5.Name = "groupBox5";
			this.groupBox5.Size = new System.Drawing.Size(480, 384);
			this.groupBox5.TabIndex = 37;
			this.groupBox5.TabStop = false;
			this.groupBox5.Text = "Image filtering / character separation";
			// 
			// onGrayCheckBox
			// 
			this.onGrayCheckBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.onGrayCheckBox.Checked = true;
			this.onGrayCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
			this.onGrayCheckBox.Location = new System.Drawing.Point(16, 264);
			this.onGrayCheckBox.Name = "onGrayCheckBox";
			this.onGrayCheckBox.Size = new System.Drawing.Size(152, 16);
			this.onGrayCheckBox.TabIndex = 25;
			this.onGrayCheckBox.Text = "1. Gray conversion";
			// 
			// srcImgLab
			// 
			this.srcImgLab.Location = new System.Drawing.Point(16, 16);
			this.srcImgLab.Name = "srcImgLab";
			this.srcImgLab.Size = new System.Drawing.Size(96, 16);
			this.srcImgLab.TabIndex = 10;
			this.srcImgLab.Text = "Original image:";
			// 
			// srcImgPB
			// 
			this.srcImgPB.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
				| System.Windows.Forms.AnchorStyles.Left) 
				| System.Windows.Forms.AnchorStyles.Right)));
			this.srcImgPB.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.srcImgPB.Location = new System.Drawing.Point(16, 32);
			this.srcImgPB.Name = "srcImgPB";
			this.srcImgPB.Size = new System.Drawing.Size(160, 176);
			this.srcImgPB.TabIndex = 9;
			this.srcImgPB.TabStop = false;
			this.srcImgPB.Paint += new System.Windows.Forms.PaintEventHandler(this.srcImgPB_Paint);
			// 
			// filteredImgPB
			// 
			this.filteredImgPB.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.filteredImgPB.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.filteredImgPB.Location = new System.Drawing.Point(312, 32);
			this.filteredImgPB.Name = "filteredImgPB";
			this.filteredImgPB.Size = new System.Drawing.Size(160, 176);
			this.filteredImgPB.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
			this.filteredImgPB.TabIndex = 13;
			this.filteredImgPB.TabStop = false;
			// 
			// filteredImgLab
			// 
			this.filteredImgLab.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.filteredImgLab.Location = new System.Drawing.Point(312, 16);
			this.filteredImgLab.Name = "filteredImgLab";
			this.filteredImgLab.Size = new System.Drawing.Size(128, 16);
			this.filteredImgLab.TabIndex = 14;
			this.filteredImgLab.Text = "Filtered image:";
			// 
			// cannyGroupBox
			// 
			this.cannyGroupBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.cannyGroupBox.Controls.Add(this.onGaussCheckBox);
			this.cannyGroupBox.Controls.Add(this.onCannyCheckBox);
			this.cannyGroupBox.Controls.Add(this.highThreshnUpDown);
			this.cannyGroupBox.Controls.Add(this.lowThreshnUpDown);
			this.cannyGroupBox.Controls.Add(this.highThreshLab);
			this.cannyGroupBox.Controls.Add(this.lowThreshLab);
			this.cannyGroupBox.Controls.Add(this.sigmanUpDown);
			this.cannyGroupBox.Controls.Add(this.sigmaLab);
			this.cannyGroupBox.Location = new System.Drawing.Point(192, 264);
			this.cannyGroupBox.Name = "cannyGroupBox";
			this.cannyGroupBox.Size = new System.Drawing.Size(280, 88);
			this.cannyGroupBox.TabIndex = 42;
			this.cannyGroupBox.TabStop = false;
			this.cannyGroupBox.Text = "Canny algorithm properties";
			// 
			// onGaussCheckBox
			// 
			this.onGaussCheckBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.onGaussCheckBox.Location = new System.Drawing.Point(160, 20);
			this.onGaussCheckBox.Name = "onGaussCheckBox";
			this.onGaussCheckBox.Size = new System.Drawing.Size(112, 16);
			this.onGaussCheckBox.TabIndex = 31;
			this.onGaussCheckBox.Text = "5. Smoothing";
			// 
			// onCannyCheckBox
			// 
			this.onCannyCheckBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.onCannyCheckBox.Location = new System.Drawing.Point(160, 56);
			this.onCannyCheckBox.Name = "onCannyCheckBox";
			this.onCannyCheckBox.Size = new System.Drawing.Size(88, 16);
			this.onCannyCheckBox.TabIndex = 34;
			this.onCannyCheckBox.Text = "6. Canny";
			// 
			// highThreshnUpDown
			// 
			this.highThreshnUpDown.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.highThreshnUpDown.Location = new System.Drawing.Point(104, 64);
			this.highThreshnUpDown.Maximum = new System.Decimal(new int[] {
																			  255,
																			  0,
																			  0,
																			  0});
			this.highThreshnUpDown.Name = "highThreshnUpDown";
			this.highThreshnUpDown.Size = new System.Drawing.Size(48, 20);
			this.highThreshnUpDown.TabIndex = 33;
			// 
			// lowThreshnUpDown
			// 
			this.lowThreshnUpDown.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.lowThreshnUpDown.Location = new System.Drawing.Point(104, 40);
			this.lowThreshnUpDown.Maximum = new System.Decimal(new int[] {
																			 255,
																			 0,
																			 0,
																			 0});
			this.lowThreshnUpDown.Name = "lowThreshnUpDown";
			this.lowThreshnUpDown.Size = new System.Drawing.Size(48, 20);
			this.lowThreshnUpDown.TabIndex = 32;
			// 
			// highThreshLab
			// 
			this.highThreshLab.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.highThreshLab.Location = new System.Drawing.Point(16, 68);
			this.highThreshLab.Name = "highThreshLab";
			this.highThreshLab.Size = new System.Drawing.Size(84, 16);
			this.highThreshLab.TabIndex = 27;
			this.highThreshLab.Text = "High threshold:";
			this.highThreshLab.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// lowThreshLab
			// 
			this.lowThreshLab.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.lowThreshLab.Location = new System.Drawing.Point(16, 44);
			this.lowThreshLab.Name = "lowThreshLab";
			this.lowThreshLab.Size = new System.Drawing.Size(84, 16);
			this.lowThreshLab.TabIndex = 26;
			this.lowThreshLab.Text = "Low threshold:";
			this.lowThreshLab.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// sigmanUpDown
			// 
			this.sigmanUpDown.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.sigmanUpDown.DecimalPlaces = 2;
			this.sigmanUpDown.Increment = new System.Decimal(new int[] {
																		   1,
																		   0,
																		   0,
																		   65536});
			this.sigmanUpDown.Location = new System.Drawing.Point(104, 16);
			this.sigmanUpDown.Maximum = new System.Decimal(new int[] {
																		 99,
																		 0,
																		 0,
																		 65536});
			this.sigmanUpDown.Name = "sigmanUpDown";
			this.sigmanUpDown.Size = new System.Drawing.Size(48, 20);
			this.sigmanUpDown.TabIndex = 30;
			// 
			// sigmaLab
			// 
			this.sigmaLab.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.sigmaLab.Location = new System.Drawing.Point(16, 20);
			this.sigmaLab.Name = "sigmaLab";
			this.sigmaLab.Size = new System.Drawing.Size(88, 16);
			this.sigmaLab.TabIndex = 20;
			this.sigmaLab.Text = "Sigma (Gauss):";
			this.sigmaLab.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// onBrightnCheckBox
			// 
			this.onBrightnCheckBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.onBrightnCheckBox.Location = new System.Drawing.Point(16, 288);
			this.onBrightnCheckBox.Name = "onBrightnCheckBox";
			this.onBrightnCheckBox.Size = new System.Drawing.Size(184, 16);
			this.onBrightnCheckBox.TabIndex = 26;
			this.onBrightnCheckBox.Text = "2. Brightness normalization";
			// 
			// onHistCheckBox
			// 
			this.onHistCheckBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.onHistCheckBox.Location = new System.Drawing.Point(16, 312);
			this.onHistCheckBox.Name = "onHistCheckBox";
			this.onHistCheckBox.Size = new System.Drawing.Size(184, 16);
			this.onHistCheckBox.TabIndex = 27;
			this.onHistCheckBox.Text = "3. Histogramm equalization";
			// 
			// onBinCheckBox
			// 
			this.onBinCheckBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.onBinCheckBox.Checked = true;
			this.onBinCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
			this.onBinCheckBox.Location = new System.Drawing.Point(16, 336);
			this.onBinCheckBox.Name = "onBinCheckBox";
			this.onBinCheckBox.Size = new System.Drawing.Size(152, 16);
			this.onBinCheckBox.TabIndex = 28;
			this.onBinCheckBox.Text = "4. Binary conversion";
			// 
			// binThreshLab
			// 
			this.binThreshLab.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.binThreshLab.Location = new System.Drawing.Point(32, 356);
			this.binThreshLab.Name = "binThreshLab";
			this.binThreshLab.Size = new System.Drawing.Size(75, 16);
			this.binThreshLab.TabIndex = 30;
			this.binThreshLab.Text = "b/w threshold:";
			// 
			// binUpDown
			// 
			this.binUpDown.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.binUpDown.DecimalPlaces = 3;
			this.binUpDown.Increment = new System.Decimal(new int[] {
																		1,
																		0,
																		0,
																		131072});
			this.binUpDown.Location = new System.Drawing.Point(120, 352);
			this.binUpDown.Maximum = new System.Decimal(new int[] {
																	  1,
																	  0,
																	  0,
																	  0});
			this.binUpDown.Name = "binUpDown";
			this.binUpDown.Size = new System.Drawing.Size(48, 20);
			this.binUpDown.TabIndex = 29;
			// 
			// segmRgnGroupBox
			// 
			this.segmRgnGroupBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.segmRgnGroupBox.Controls.Add(this.heightCharRgnnUpDown);
			this.segmRgnGroupBox.Controls.Add(this.heightCharRgnLab);
			this.segmRgnGroupBox.Controls.Add(this.widthCharRgnnUpDown);
			this.segmRgnGroupBox.Controls.Add(this.widthCharRgnLab);
			this.segmRgnGroupBox.Controls.Add(this.yCharRgnnUpDown);
			this.segmRgnGroupBox.Controls.Add(this.yCharRgnLab);
			this.segmRgnGroupBox.Controls.Add(this.xCharRgnnUpDown);
			this.segmRgnGroupBox.Controls.Add(this.xCharRgnLab);
			this.segmRgnGroupBox.Location = new System.Drawing.Point(184, 32);
			this.segmRgnGroupBox.Name = "segmRgnGroupBox";
			this.segmRgnGroupBox.Size = new System.Drawing.Size(112, 128);
			this.segmRgnGroupBox.TabIndex = 40;
			this.segmRgnGroupBox.TabStop = false;
			this.segmRgnGroupBox.Text = "Characters region";
			this.toolTip.SetToolTip(this.segmRgnGroupBox, "choose the rectangle which will be evaluated by the extraction algorithm");
			// 
			// heightCharRgnnUpDown
			// 
			this.heightCharRgnnUpDown.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.heightCharRgnnUpDown.Location = new System.Drawing.Point(56, 96);
			this.heightCharRgnnUpDown.Maximum = new System.Decimal(new int[] {
																				 100000,
																				 0,
																				 0,
																				 0});
			this.heightCharRgnnUpDown.Minimum = new System.Decimal(new int[] {
																				 1,
																				 0,
																				 0,
																				 0});
			this.heightCharRgnnUpDown.Name = "heightCharRgnnUpDown";
			this.heightCharRgnnUpDown.Size = new System.Drawing.Size(48, 20);
			this.heightCharRgnnUpDown.TabIndex = 24;
			this.heightCharRgnnUpDown.Value = new System.Decimal(new int[] {
																			   1,
																			   0,
																			   0,
																			   0});
			this.heightCharRgnnUpDown.ValueChanged += new System.EventHandler(this.charRgn_ValueChanged);
			// 
			// heightCharRgnLab
			// 
			this.heightCharRgnLab.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.heightCharRgnLab.Location = new System.Drawing.Point(8, 100);
			this.heightCharRgnLab.Name = "heightCharRgnLab";
			this.heightCharRgnLab.Size = new System.Drawing.Size(40, 16);
			this.heightCharRgnLab.TabIndex = 6;
			this.heightCharRgnLab.Text = "Height:";
			this.heightCharRgnLab.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// widthCharRgnnUpDown
			// 
			this.widthCharRgnnUpDown.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.widthCharRgnnUpDown.Location = new System.Drawing.Point(56, 72);
			this.widthCharRgnnUpDown.Maximum = new System.Decimal(new int[] {
																				100000,
																				0,
																				0,
																				0});
			this.widthCharRgnnUpDown.Minimum = new System.Decimal(new int[] {
																				1,
																				0,
																				0,
																				0});
			this.widthCharRgnnUpDown.Name = "widthCharRgnnUpDown";
			this.widthCharRgnnUpDown.Size = new System.Drawing.Size(48, 20);
			this.widthCharRgnnUpDown.TabIndex = 23;
			this.widthCharRgnnUpDown.Value = new System.Decimal(new int[] {
																			  1,
																			  0,
																			  0,
																			  0});
			this.widthCharRgnnUpDown.ValueChanged += new System.EventHandler(this.charRgn_ValueChanged);
			// 
			// widthCharRgnLab
			// 
			this.widthCharRgnLab.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.widthCharRgnLab.Location = new System.Drawing.Point(8, 76);
			this.widthCharRgnLab.Name = "widthCharRgnLab";
			this.widthCharRgnLab.Size = new System.Drawing.Size(40, 16);
			this.widthCharRgnLab.TabIndex = 4;
			this.widthCharRgnLab.Text = "Width:";
			this.widthCharRgnLab.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// yCharRgnnUpDown
			// 
			this.yCharRgnnUpDown.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.yCharRgnnUpDown.Location = new System.Drawing.Point(56, 48);
			this.yCharRgnnUpDown.Maximum = new System.Decimal(new int[] {
																			100000,
																			0,
																			0,
																			0});
			this.yCharRgnnUpDown.Name = "yCharRgnnUpDown";
			this.yCharRgnnUpDown.Size = new System.Drawing.Size(48, 20);
			this.yCharRgnnUpDown.TabIndex = 22;
			this.yCharRgnnUpDown.ValueChanged += new System.EventHandler(this.charRgn_ValueChanged);
			// 
			// yCharRgnLab
			// 
			this.yCharRgnLab.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.yCharRgnLab.Location = new System.Drawing.Point(8, 52);
			this.yCharRgnLab.Name = "yCharRgnLab";
			this.yCharRgnLab.Size = new System.Drawing.Size(40, 16);
			this.yCharRgnLab.TabIndex = 2;
			this.yCharRgnLab.Text = "y:";
			this.yCharRgnLab.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// xCharRgnnUpDown
			// 
			this.xCharRgnnUpDown.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.xCharRgnnUpDown.Location = new System.Drawing.Point(56, 24);
			this.xCharRgnnUpDown.Maximum = new System.Decimal(new int[] {
																			100000,
																			0,
																			0,
																			0});
			this.xCharRgnnUpDown.Name = "xCharRgnnUpDown";
			this.xCharRgnnUpDown.Size = new System.Drawing.Size(48, 20);
			this.xCharRgnnUpDown.TabIndex = 21;
			this.xCharRgnnUpDown.ValueChanged += new System.EventHandler(this.charRgn_ValueChanged);
			// 
			// xCharRgnLab
			// 
			this.xCharRgnLab.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.xCharRgnLab.Location = new System.Drawing.Point(8, 28);
			this.xCharRgnLab.Name = "xCharRgnLab";
			this.xCharRgnLab.Size = new System.Drawing.Size(40, 16);
			this.xCharRgnLab.TabIndex = 0;
			this.xCharRgnLab.Text = "x:";
			this.xCharRgnLab.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// featureExtractGroupBox
			// 
			this.featureExtractGroupBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.featureExtractGroupBox.Controls.Add(this.label11);
			this.featureExtractGroupBox.Controls.Add(this.featureExtraktioncomboBox);
			this.featureExtractGroupBox.Controls.Add(this.counterGroupBox);
			this.featureExtractGroupBox.Controls.Add(this.segmGroupBox);
			this.featureExtractGroupBox.Location = new System.Drawing.Point(704, 16);
			this.featureExtractGroupBox.Name = "featureExtractGroupBox";
			this.featureExtractGroupBox.Size = new System.Drawing.Size(232, 176);
			this.featureExtractGroupBox.TabIndex = 31;
			this.featureExtractGroupBox.TabStop = false;
			this.featureExtractGroupBox.Text = "OCR feature extraction";
			// 
			// label11
			// 
			this.label11.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.label11.Location = new System.Drawing.Point(8, 32);
			this.label11.Name = "label11";
			this.label11.Size = new System.Drawing.Size(144, 16);
			this.label11.TabIndex = 21;
			this.label11.Text = "Feature extraction method:";
			// 
			// featureExtraktioncomboBox
			// 
			this.featureExtraktioncomboBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.featureExtraktioncomboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.featureExtraktioncomboBox.Location = new System.Drawing.Point(8, 48);
			this.featureExtraktioncomboBox.Name = "featureExtraktioncomboBox";
			this.featureExtraktioncomboBox.Size = new System.Drawing.Size(216, 21);
			this.featureExtraktioncomboBox.TabIndex = 55;
			this.toolTip.SetToolTip(this.featureExtraktioncomboBox, "select the feature extraction algorithm");
			this.featureExtraktioncomboBox.SelectedIndexChanged += new System.EventHandler(this.featureExtraktioncomboBox_SelectedIndexChanged);
			// 
			// counterGroupBox
			// 
			this.counterGroupBox.Controls.Add(this.dyUpDown);
			this.counterGroupBox.Controls.Add(this.dyLab);
			this.counterGroupBox.Controls.Add(this.dxUpDown);
			this.counterGroupBox.Controls.Add(this.dxLab);
			this.counterGroupBox.Location = new System.Drawing.Point(24, 88);
			this.counterGroupBox.Name = "counterGroupBox";
			this.counterGroupBox.Size = new System.Drawing.Size(184, 72);
			this.counterGroupBox.TabIndex = 37;
			this.counterGroupBox.TabStop = false;
			this.counterGroupBox.Text = "Row (dy)  / column (dx) jumps";
			this.counterGroupBox.Visible = false;
			// 
			// dyUpDown
			// 
			this.dyUpDown.Location = new System.Drawing.Point(120, 24);
			this.dyUpDown.Minimum = new System.Decimal(new int[] {
																	 1,
																	 0,
																	 0,
																	 0});
			this.dyUpDown.Name = "dyUpDown";
			this.dyUpDown.Size = new System.Drawing.Size(40, 20);
			this.dyUpDown.TabIndex = 57;
			this.dyUpDown.Value = new System.Decimal(new int[] {
																   1,
																   0,
																   0,
																   0});
			// 
			// dyLab
			// 
			this.dyLab.Location = new System.Drawing.Point(96, 26);
			this.dyLab.Name = "dyLab";
			this.dyLab.Size = new System.Drawing.Size(16, 16);
			this.dyLab.TabIndex = 25;
			this.dyLab.Text = "dy";
			// 
			// dxUpDown
			// 
			this.dxUpDown.Location = new System.Drawing.Point(32, 24);
			this.dxUpDown.Minimum = new System.Decimal(new int[] {
																	 1,
																	 0,
																	 0,
																	 0});
			this.dxUpDown.Name = "dxUpDown";
			this.dxUpDown.Size = new System.Drawing.Size(40, 20);
			this.dxUpDown.TabIndex = 56;
			this.dxUpDown.Value = new System.Decimal(new int[] {
																   1,
																   0,
																   0,
																   0});
			// 
			// dxLab
			// 
			this.dxLab.Location = new System.Drawing.Point(8, 26);
			this.dxLab.Name = "dxLab";
			this.dxLab.Size = new System.Drawing.Size(16, 16);
			this.dxLab.TabIndex = 23;
			this.dxLab.Text = "dx";
			// 
			// segmGroupBox
			// 
			this.segmGroupBox.Controls.Add(this.segmLab);
			this.segmGroupBox.Controls.Add(this.segmUpDown);
			this.segmGroupBox.Location = new System.Drawing.Point(24, 88);
			this.segmGroupBox.Name = "segmGroupBox";
			this.segmGroupBox.Size = new System.Drawing.Size(184, 72);
			this.segmGroupBox.TabIndex = 38;
			this.segmGroupBox.TabStop = false;
			this.segmGroupBox.Text = "Segmentnumber";
			this.segmGroupBox.Visible = false;
			// 
			// segmLab
			// 
			this.segmLab.Location = new System.Drawing.Point(8, 16);
			this.segmLab.Name = "segmLab";
			this.segmLab.Size = new System.Drawing.Size(168, 16);
			this.segmLab.TabIndex = 23;
			this.segmLab.Text = "No. of segments for row + cols";
			// 
			// segmUpDown
			// 
			this.segmUpDown.Location = new System.Drawing.Point(72, 40);
			this.segmUpDown.Minimum = new System.Decimal(new int[] {
																	   1,
																	   0,
																	   0,
																	   0});
			this.segmUpDown.Name = "segmUpDown";
			this.segmUpDown.Size = new System.Drawing.Size(40, 20);
			this.segmUpDown.TabIndex = 58;
			this.segmUpDown.Value = new System.Decimal(new int[] {
																	 1,
																	 0,
																	 0,
																	 0});
			// 
			// groupBox3
			// 
			this.groupBox3.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
				| System.Windows.Forms.AnchorStyles.Right)));
			this.groupBox3.Controls.Add(this.noiseAdjuster);
			this.groupBox3.Controls.Add(this.selectFontBtn);
			this.groupBox3.Controls.Add(this.noisyImagesNumUpDown);
			this.groupBox3.Controls.Add(this.label17);
			this.groupBox3.Controls.Add(this.label16);
			this.groupBox3.Controls.Add(this.label15);
			this.groupBox3.Controls.Add(this.label14);
			this.groupBox3.Controls.Add(this.letterRangeMinUpDown);
			this.groupBox3.Controls.Add(this.letterRangeMaxUpDown);
			this.groupBox3.Location = new System.Drawing.Point(16, 408);
			this.groupBox3.Name = "groupBox3";
			this.groupBox3.Size = new System.Drawing.Size(480, 104);
			this.groupBox3.TabIndex = 30;
			this.groupBox3.TabStop = false;
			this.groupBox3.Text = "OCR training-image creation";
			// 
			// noiseAdjuster
			// 
			this.noiseAdjuster.Location = new System.Drawing.Point(16, 80);
			this.noiseAdjuster.maxProp = 50F;
			this.noiseAdjuster.minProp = 0F;
			this.noiseAdjuster.Name = "noiseAdjuster";
			this.noiseAdjuster.paramNameProp = "Noise in image [%]";
			this.noiseAdjuster.roundingProp = 1;
			this.noiseAdjuster.Size = new System.Drawing.Size(448, 16);
			this.noiseAdjuster.stepProp = 0.1F;
			this.noiseAdjuster.TabIndex = 41;
			this.noiseAdjuster.valueProp = 1F;
			// 
			// selectFontBtn
			// 
			this.selectFontBtn.Location = new System.Drawing.Point(344, 44);
			this.selectFontBtn.Name = "selectFontBtn";
			this.selectFontBtn.Size = new System.Drawing.Size(120, 24);
			this.selectFontBtn.TabIndex = 40;
			this.selectFontBtn.Text = "Select font";
			this.selectFontBtn.Click += new System.EventHandler(this.selectFontBtn_Click);
			// 
			// noisyImagesNumUpDown
			// 
			this.noisyImagesNumUpDown.Location = new System.Drawing.Point(168, 48);
			this.noisyImagesNumUpDown.Maximum = new System.Decimal(new int[] {
																				 250,
																				 0,
																				 0,
																				 0});
			this.noisyImagesNumUpDown.Minimum = new System.Decimal(new int[] {
																				 1,
																				 0,
																				 0,
																				 0});
			this.noisyImagesNumUpDown.Name = "noisyImagesNumUpDown";
			this.noisyImagesNumUpDown.Size = new System.Drawing.Size(152, 20);
			this.noisyImagesNumUpDown.TabIndex = 39;
			this.noisyImagesNumUpDown.Value = new System.Decimal(new int[] {
																			   1,
																			   0,
																			   0,
																			   0});
			// 
			// label17
			// 
			this.label17.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.label17.Location = new System.Drawing.Point(168, 24);
			this.label17.Name = "label17";
			this.label17.Size = new System.Drawing.Size(152, 16);
			this.label17.TabIndex = 29;
			this.label17.Text = "Images for each character:";
			// 
			// label16
			// 
			this.label16.Location = new System.Drawing.Point(88, 50);
			this.label16.Name = "label16";
			this.label16.Size = new System.Drawing.Size(16, 16);
			this.label16.TabIndex = 28;
			this.label16.Text = "to";
			// 
			// label15
			// 
			this.label15.Location = new System.Drawing.Point(16, 50);
			this.label15.Name = "label15";
			this.label15.Size = new System.Drawing.Size(32, 16);
			this.label15.TabIndex = 27;
			this.label15.Text = "from";
			// 
			// label14
			// 
			this.label14.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.label14.Location = new System.Drawing.Point(16, 24);
			this.label14.Name = "label14";
			this.label14.Size = new System.Drawing.Size(120, 16);
			this.label14.TabIndex = 26;
			this.label14.Text = "Character range:";
			// 
			// letterRangeMinUpDown
			// 
			this.letterRangeMinUpDown.Items.Add("A");
			this.letterRangeMinUpDown.Items.Add("B");
			this.letterRangeMinUpDown.Items.Add("C");
			this.letterRangeMinUpDown.Items.Add("D");
			this.letterRangeMinUpDown.Items.Add("E");
			this.letterRangeMinUpDown.Items.Add("F");
			this.letterRangeMinUpDown.Items.Add("G");
			this.letterRangeMinUpDown.Items.Add("H");
			this.letterRangeMinUpDown.Items.Add("I");
			this.letterRangeMinUpDown.Items.Add("J");
			this.letterRangeMinUpDown.Items.Add("K");
			this.letterRangeMinUpDown.Items.Add("L");
			this.letterRangeMinUpDown.Items.Add("M");
			this.letterRangeMinUpDown.Items.Add("N");
			this.letterRangeMinUpDown.Items.Add("O");
			this.letterRangeMinUpDown.Items.Add("P");
			this.letterRangeMinUpDown.Items.Add("Q");
			this.letterRangeMinUpDown.Items.Add("R");
			this.letterRangeMinUpDown.Items.Add("S");
			this.letterRangeMinUpDown.Items.Add("T");
			this.letterRangeMinUpDown.Items.Add("U");
			this.letterRangeMinUpDown.Items.Add("V");
			this.letterRangeMinUpDown.Items.Add("W");
			this.letterRangeMinUpDown.Items.Add("X");
			this.letterRangeMinUpDown.Items.Add("Y");
			this.letterRangeMinUpDown.Items.Add("Z");
			this.letterRangeMinUpDown.Location = new System.Drawing.Point(48, 48);
			this.letterRangeMinUpDown.Name = "letterRangeMinUpDown";
			this.letterRangeMinUpDown.ReadOnly = true;
			this.letterRangeMinUpDown.Size = new System.Drawing.Size(40, 20);
			this.letterRangeMinUpDown.TabIndex = 37;
			this.letterRangeMinUpDown.Text = "A";
			// 
			// letterRangeMaxUpDown
			// 
			this.letterRangeMaxUpDown.Items.Add("A");
			this.letterRangeMaxUpDown.Items.Add("B");
			this.letterRangeMaxUpDown.Items.Add("C");
			this.letterRangeMaxUpDown.Items.Add("D");
			this.letterRangeMaxUpDown.Items.Add("E");
			this.letterRangeMaxUpDown.Items.Add("F");
			this.letterRangeMaxUpDown.Items.Add("G");
			this.letterRangeMaxUpDown.Items.Add("H");
			this.letterRangeMaxUpDown.Items.Add("I");
			this.letterRangeMaxUpDown.Items.Add("J");
			this.letterRangeMaxUpDown.Items.Add("K");
			this.letterRangeMaxUpDown.Items.Add("L");
			this.letterRangeMaxUpDown.Items.Add("M");
			this.letterRangeMaxUpDown.Items.Add("N");
			this.letterRangeMaxUpDown.Items.Add("O");
			this.letterRangeMaxUpDown.Items.Add("P");
			this.letterRangeMaxUpDown.Items.Add("Q");
			this.letterRangeMaxUpDown.Items.Add("R");
			this.letterRangeMaxUpDown.Items.Add("S");
			this.letterRangeMaxUpDown.Items.Add("T");
			this.letterRangeMaxUpDown.Items.Add("U");
			this.letterRangeMaxUpDown.Items.Add("V");
			this.letterRangeMaxUpDown.Items.Add("W");
			this.letterRangeMaxUpDown.Items.Add("X");
			this.letterRangeMaxUpDown.Items.Add("Y");
			this.letterRangeMaxUpDown.Items.Add("Z");
			this.letterRangeMaxUpDown.Location = new System.Drawing.Point(104, 48);
			this.letterRangeMaxUpDown.Name = "letterRangeMaxUpDown";
			this.letterRangeMaxUpDown.ReadOnly = true;
			this.letterRangeMaxUpDown.Size = new System.Drawing.Size(40, 20);
			this.letterRangeMaxUpDown.TabIndex = 38;
			this.letterRangeMaxUpDown.Text = "D";
			// 
			// filterRadioBtn
			// 
			this.filterRadioBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.filterRadioBtn.Location = new System.Drawing.Point(496, 200);
			this.filterRadioBtn.Name = "filterRadioBtn";
			this.filterRadioBtn.Size = new System.Drawing.Size(16, 16);
			this.filterRadioBtn.TabIndex = 42;
			// 
			// startGeneratePatternsBtn
			// 
			this.startGeneratePatternsBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.startGeneratePatternsBtn.BackColor = System.Drawing.SystemColors.Control;
			this.startGeneratePatternsBtn.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.startGeneratePatternsBtn.ForeColor = System.Drawing.SystemColors.ControlText;
			this.startGeneratePatternsBtn.Location = new System.Drawing.Point(496, 424);
			this.startGeneratePatternsBtn.Name = "startGeneratePatternsBtn";
			this.startGeneratePatternsBtn.Size = new System.Drawing.Size(40, 24);
			this.startGeneratePatternsBtn.TabIndex = 44;
			this.startGeneratePatternsBtn.Text = "Start";
			this.toolTip.SetToolTip(this.startGeneratePatternsBtn, "Start creation of OCR training images or if \'Image filtering\' start filtering.\\nA" +
				"fter that extract the feature vector.");
			this.startGeneratePatternsBtn.Click += new System.EventHandler(this.startFilterBtn_Click);
			// 
			// stopGeneratePatternsBtn
			// 
			this.stopGeneratePatternsBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.stopGeneratePatternsBtn.Enabled = false;
			this.stopGeneratePatternsBtn.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.stopGeneratePatternsBtn.Location = new System.Drawing.Point(496, 472);
			this.stopGeneratePatternsBtn.Name = "stopGeneratePatternsBtn";
			this.stopGeneratePatternsBtn.Size = new System.Drawing.Size(40, 24);
			this.stopGeneratePatternsBtn.TabIndex = 45;
			this.stopGeneratePatternsBtn.Text = "Abort";
			this.toolTip.SetToolTip(this.stopGeneratePatternsBtn, "Abort/cancel filtering, creation.");
			this.stopGeneratePatternsBtn.Click += new System.EventHandler(this.stopFilterBtn_Click);
			// 
			// trainPage
			// 
			this.trainPage.Controls.Add(this.slowmoAdjuster);
			this.trainPage.Controls.Add(this.steepnessAdjuster);
			this.trainPage.Controls.Add(this.maxErrorUpDown);
			this.trainPage.Controls.Add(this.maxErrorLab);
			this.trainPage.Controls.Add(this.cyclesUpDown);
			this.trainPage.Controls.Add(this.cyclesLabel);
			this.trainPage.Controls.Add(this.learnAlgoLabel);
			this.trainPage.Controls.Add(this.learnAlgoComboBox);
			this.trainPage.Controls.Add(this.stopTrainBtn);
			this.trainPage.Controls.Add(this.fastModeCheckBox);
			this.trainPage.Controls.Add(this.startTrainBtn);
			this.trainPage.Controls.Add(this.errorListView);
			this.trainPage.Controls.Add(this.errorListViewLabel);
			this.trainPage.Controls.Add(this.errorGraphLabel);
			this.trainPage.Controls.Add(this.errorPB);
			this.trainPage.Controls.Add(this.fastModeLabel);
			this.trainPage.Controls.Add(this.currentErrorTxt);
			this.trainPage.Location = new System.Drawing.Point(4, 22);
			this.trainPage.Name = "trainPage";
			this.trainPage.Size = new System.Drawing.Size(944, 590);
			this.trainPage.TabIndex = 0;
			this.trainPage.Text = "Net Trainer";
			// 
			// slowmoAdjuster
			// 
			this.slowmoAdjuster.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.slowmoAdjuster.Location = new System.Drawing.Point(312, 516);
			this.slowmoAdjuster.maxProp = 1000F;
			this.slowmoAdjuster.minProp = 0F;
			this.slowmoAdjuster.Name = "slowmoAdjuster";
			this.slowmoAdjuster.paramNameProp = "Slow-motion (ms)";
			this.slowmoAdjuster.roundingProp = 0;
			this.slowmoAdjuster.Size = new System.Drawing.Size(192, 16);
			this.slowmoAdjuster.stepProp = 1F;
			this.slowmoAdjuster.TabIndex = 79;
			this.slowmoAdjuster.valueProp = 0F;
			// 
			// steepnessAdjuster
			// 
			this.steepnessAdjuster.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
				| System.Windows.Forms.AnchorStyles.Right)));
			this.steepnessAdjuster.Cursor = System.Windows.Forms.Cursors.Default;
			this.steepnessAdjuster.Location = new System.Drawing.Point(520, 488);
			this.steepnessAdjuster.maxProp = 10F;
			this.steepnessAdjuster.minProp = 0F;
			this.steepnessAdjuster.Name = "steepnessAdjuster";
			this.steepnessAdjuster.paramNameProp = "Steepness ActFunc";
			this.steepnessAdjuster.roundingProp = 2;
			this.steepnessAdjuster.Size = new System.Drawing.Size(408, 16);
			this.steepnessAdjuster.stepProp = 0.1F;
			this.steepnessAdjuster.TabIndex = 84;
			this.toolTip.SetToolTip(this.steepnessAdjuster, "adjust the steepness of the activation function for all neurons");
			this.steepnessAdjuster.valueProp = 0.9F;
			this.steepnessAdjuster.onNewValue += new ParamAdjuster.valueHandler(this.steepnessAdjuster_onNewValue);
			// 
			// maxErrorUpDown
			// 
			this.maxErrorUpDown.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.maxErrorUpDown.DecimalPlaces = 7;
			this.maxErrorUpDown.Increment = new System.Decimal(new int[] {
																			 1,
																			 0,
																			 0,
																			 262144});
			this.maxErrorUpDown.Location = new System.Drawing.Point(232, 569);
			this.maxErrorUpDown.Maximum = new System.Decimal(new int[] {
																		   100000,
																		   0,
																		   0,
																		   0});
			this.maxErrorUpDown.Name = "maxErrorUpDown";
			this.maxErrorUpDown.Size = new System.Drawing.Size(72, 20);
			this.maxErrorUpDown.TabIndex = 77;
			this.maxErrorUpDown.ValueChanged += new System.EventHandler(this.maxErrorUpDown_ValueChanged);
			// 
			// maxErrorLab
			// 
			this.maxErrorLab.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.maxErrorLab.Location = new System.Drawing.Point(176, 571);
			this.maxErrorLab.Name = "maxErrorLab";
			this.maxErrorLab.Size = new System.Drawing.Size(64, 16);
			this.maxErrorLab.TabIndex = 29;
			this.maxErrorLab.Text = "Max error:";
			this.toolTip.SetToolTip(this.maxErrorLab, "set the error when the training should stop");
			// 
			// cyclesUpDown
			// 
			this.cyclesUpDown.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.cyclesUpDown.Location = new System.Drawing.Point(432, 539);
			this.cyclesUpDown.Maximum = new System.Decimal(new int[] {
																		 99999999,
																		 0,
																		 0,
																		 0});
			this.cyclesUpDown.Minimum = new System.Decimal(new int[] {
																		 1,
																		 0,
																		 0,
																		 0});
			this.cyclesUpDown.Name = "cyclesUpDown";
			this.cyclesUpDown.Size = new System.Drawing.Size(72, 20);
			this.cyclesUpDown.TabIndex = 80;
			this.toolTip.SetToolTip(this.cyclesUpDown, "Press <Enter> to update typed changes");
			this.cyclesUpDown.Value = new System.Decimal(new int[] {
																	   12345678,
																	   0,
																	   0,
																	   0});
			this.cyclesUpDown.ValueChanged += new System.EventHandler(this.cyclesUpDown_ValueChanged);
			// 
			// cyclesLabel
			// 
			this.cyclesLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.cyclesLabel.Location = new System.Drawing.Point(312, 543);
			this.cyclesLabel.Name = "cyclesLabel";
			this.cyclesLabel.Size = new System.Drawing.Size(192, 16);
			this.cyclesLabel.TabIndex = 23;
			this.cyclesLabel.Text = "Max cycles:";
			this.toolTip.SetToolTip(this.cyclesLabel, "Press <Enter> to update typed changes");
			// 
			// learnAlgoLabel
			// 
			this.learnAlgoLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.learnAlgoLabel.Location = new System.Drawing.Point(312, 472);
			this.learnAlgoLabel.Name = "learnAlgoLabel";
			this.learnAlgoLabel.Size = new System.Drawing.Size(184, 16);
			this.learnAlgoLabel.TabIndex = 19;
			this.learnAlgoLabel.Text = "Choose a learning algorithm:";
			// 
			// learnAlgoComboBox
			// 
			this.learnAlgoComboBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.learnAlgoComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.learnAlgoComboBox.Items.AddRange(new object[] {
																   "Backpropagation (+Momentum, online)",
																   "Backpropagation (+Momentum, batch)",
																   "Genetic Algorithm"});
			this.learnAlgoComboBox.Location = new System.Drawing.Point(312, 488);
			this.learnAlgoComboBox.Name = "learnAlgoComboBox";
			this.learnAlgoComboBox.Size = new System.Drawing.Size(192, 20);
			this.learnAlgoComboBox.TabIndex = 78;
			this.learnAlgoComboBox.SelectionChangeCommitted += new System.EventHandler(this.learnAlgoComboBox_SelectedIndexChanged);
			// 
			// stopTrainBtn
			// 
			this.stopTrainBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.stopTrainBtn.Location = new System.Drawing.Point(424, 565);
			this.stopTrainBtn.Name = "stopTrainBtn";
			this.stopTrainBtn.Size = new System.Drawing.Size(80, 24);
			this.stopTrainBtn.TabIndex = 82;
			this.stopTrainBtn.Text = "Stop training";
			this.stopTrainBtn.Click += new System.EventHandler(this.stopTrainBtn_Click);
			// 
			// fastModeCheckBox
			// 
			this.fastModeCheckBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.fastModeCheckBox.Location = new System.Drawing.Point(912, 464);
			this.fastModeCheckBox.Name = "fastModeCheckBox";
			this.fastModeCheckBox.Size = new System.Drawing.Size(16, 16);
			this.fastModeCheckBox.TabIndex = 15;
			this.fastModeCheckBox.Text = "fastModeCheckBox";
			// 
			// startTrainBtn
			// 
			this.startTrainBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.startTrainBtn.Location = new System.Drawing.Point(312, 565);
			this.startTrainBtn.Name = "startTrainBtn";
			this.startTrainBtn.Size = new System.Drawing.Size(88, 24);
			this.startTrainBtn.TabIndex = 81;
			this.startTrainBtn.Text = "Start training";
			this.startTrainBtn.Click += new System.EventHandler(this.startTrainBtn_Click);
			// 
			// fastModeLabel
			// 
			this.fastModeLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.fastModeLabel.Location = new System.Drawing.Point(744, 464);
			this.fastModeLabel.Name = "fastModeLabel";
			this.fastModeLabel.Size = new System.Drawing.Size(192, 16);
			this.fastModeLabel.TabIndex = 83;
			this.fastModeLabel.Text = "Fast mode (no live visualization)";
			this.toolTip.SetToolTip(this.fastModeLabel, "disables all graphic and progress indication when checked");
			// 
			// currentErrorTxt
			// 
			this.currentErrorTxt.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.currentErrorTxt.Location = new System.Drawing.Point(8, 571);
			this.currentErrorTxt.Name = "currentErrorTxt";
			this.currentErrorTxt.Size = new System.Drawing.Size(208, 16);
			this.currentErrorTxt.TabIndex = 25;
			this.currentErrorTxt.Text = "Current global error:";
			// 
			// mainMenu
			// 
			this.mainMenu.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
																					 this.fileMenuItem,
																					 this.netMenuItem,
																					 this.infoMenuItem});
			// 
			// fileMenuItem
			// 
			this.fileMenuItem.Index = 0;
			this.fileMenuItem.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
																						 this.fileMenuNewItem,
																						 this.fileMenuLoadItem,
																						 this.fileMenuSaveItem,
																						 this.fileMenuItemStrich1,
																						 this.fileMenuLoadPattern,
																						 this.fileMenuSavePattern,
																						 this.menuItem1,
																						 this.fileMenuLoadPicItem,
																						 this.menuItem2,
																						 this.fileMenuExitItem});
			this.fileMenuItem.Text = "File";
			// 
			// fileMenuNewItem
			// 
			this.fileMenuNewItem.Index = 0;
			this.fileMenuNewItem.Shortcut = System.Windows.Forms.Shortcut.CtrlN;
			this.fileMenuNewItem.Text = "Load default neural network";
			this.fileMenuNewItem.Click += new System.EventHandler(this.fileMenuNewItem_Click);
			// 
			// fileMenuLoadItem
			// 
			this.fileMenuLoadItem.Index = 1;
			this.fileMenuLoadItem.Shortcut = System.Windows.Forms.Shortcut.CtrlO;
			this.fileMenuLoadItem.Text = "Load neural network...";
			this.fileMenuLoadItem.Click += new System.EventHandler(this.fileMenuLoadItem_Click);
			// 
			// fileMenuSaveItem
			// 
			this.fileMenuSaveItem.Index = 2;
			this.fileMenuSaveItem.Shortcut = System.Windows.Forms.Shortcut.CtrlS;
			this.fileMenuSaveItem.Text = "Save neural network as...";
			this.fileMenuSaveItem.Click += new System.EventHandler(this.fileMenuSaveItem_Click);
			// 
			// fileMenuItemStrich1
			// 
			this.fileMenuItemStrich1.Index = 3;
			this.fileMenuItemStrich1.Text = "-";
			// 
			// fileMenuLoadPattern
			// 
			this.fileMenuLoadPattern.Index = 4;
			this.fileMenuLoadPattern.Shortcut = System.Windows.Forms.Shortcut.CtrlShiftO;
			this.fileMenuLoadPattern.Text = "Load patterns...";
			this.fileMenuLoadPattern.Click += new System.EventHandler(this.fileMenuLoadPattern_Click);
			// 
			// fileMenuSavePattern
			// 
			this.fileMenuSavePattern.Index = 5;
			this.fileMenuSavePattern.Shortcut = System.Windows.Forms.Shortcut.CtrlShiftS;
			this.fileMenuSavePattern.Text = "Save patterns as...";
			this.fileMenuSavePattern.Click += new System.EventHandler(this.fileMenuSavePattern_Click);
			// 
			// menuItem1
			// 
			this.menuItem1.Index = 6;
			this.menuItem1.Text = "-";
			// 
			// fileMenuLoadPicItem
			// 
			this.fileMenuLoadPicItem.Index = 7;
			this.fileMenuLoadPicItem.Shortcut = System.Windows.Forms.Shortcut.CtrlI;
			this.fileMenuLoadPicItem.Text = "Load image...";
			this.fileMenuLoadPicItem.Click += new System.EventHandler(this.fileMenuLoadPicItem_Click);
			// 
			// menuItem2
			// 
			this.menuItem2.Index = 8;
			this.menuItem2.Text = "-";
			// 
			// fileMenuExitItem
			// 
			this.fileMenuExitItem.Index = 9;
			this.fileMenuExitItem.Shortcut = System.Windows.Forms.Shortcut.AltF4;
			this.fileMenuExitItem.Text = "Exit";
			this.fileMenuExitItem.Click += new System.EventHandler(this.fileMenuExitItem_Click);
			// 
			// netMenuItem
			// 
			this.netMenuItem.Index = 1;
			this.netMenuItem.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
																						this.netMenuRandomizeItem,
																						this.netMenuStartStopThreadItem});
			this.netMenuItem.Text = "Network";
			// 
			// netMenuRandomizeItem
			// 
			this.netMenuRandomizeItem.Index = 0;
			this.netMenuRandomizeItem.Shortcut = System.Windows.Forms.Shortcut.CtrlR;
			this.netMenuRandomizeItem.Text = "Randomize weight";
			this.netMenuRandomizeItem.Click += new System.EventHandler(this.netMenuRandomizeItem_Click);
			// 
			// netMenuStartStopThreadItem
			// 
			this.netMenuStartStopThreadItem.Index = 1;
			this.netMenuStartStopThreadItem.Shortcut = System.Windows.Forms.Shortcut.CtrlT;
			this.netMenuStartStopThreadItem.Text = "Start/stop training";
			this.netMenuStartStopThreadItem.Click += new System.EventHandler(this.netMenuStartStopThreadItem_Click);
			// 
			// infoMenuItem
			// 
			this.infoMenuItem.Index = 2;
			this.infoMenuItem.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
																						 this.infoMenuHelpItem,
																						 this.infoMenuAboutItem});
			this.infoMenuItem.Text = "?";
			// 
			// infoMenuHelpItem
			// 
			this.infoMenuHelpItem.Index = 0;
			this.infoMenuHelpItem.Shortcut = System.Windows.Forms.Shortcut.F1;
			this.infoMenuHelpItem.Text = "Help";
			this.infoMenuHelpItem.Click += new System.EventHandler(this.infoMenuHelpItem_Click);
			// 
			// infoMenuAboutItem
			// 
			this.infoMenuAboutItem.Index = 1;
			this.infoMenuAboutItem.Shortcut = System.Windows.Forms.Shortcut.F2;
			this.infoMenuAboutItem.Text = "About";
			this.infoMenuAboutItem.Click += new System.EventHandler(this.infoMenuAboutItem_Click);
			// 
			// openFileDialogNN
			// 
			this.openFileDialogNN.DefaultExt = "net";
			this.openFileDialogNN.Filter = "Neural Network Files (*.net)|*.net";
			this.openFileDialogNN.InitialDirectory = "examples/net";
			this.openFileDialogNN.RestoreDirectory = true;
			this.openFileDialogNN.Title = "Load a Neural Network File...";
			// 
			// saveFileDialogNN
			// 
			this.saveFileDialogNN.DefaultExt = "net";
			this.saveFileDialogNN.Filter = "Neural Network Files (*.net)|*.net";
			this.saveFileDialogNN.InitialDirectory = "examples/net";
			this.saveFileDialogNN.RestoreDirectory = true;
			this.saveFileDialogNN.Title = "Save the Net as  Neural Network File...";
			// 
			// openFileDialogPic
			// 
			this.openFileDialogPic.Filter = "All Supported Media|*.bmp;*.jpg;*.jpeg;*.jpe;*.gif;*.png|Windows Bitmap (*.bmp)|*" +
				".bmp|JPEG (*.jpg *.jpeg *.jpe)|*.jpg;*.jpeg;*.jpe|CompuServe GIF (*.gif)|*.gif|P" +
				"ortable Network Graphics (*.png)|*.png";
			this.openFileDialogPic.InitialDirectory = "examples/pics";
			this.openFileDialogPic.RestoreDirectory = true;
			this.openFileDialogPic.Title = "Load a Picture...";
			// 
			// batchLearnRadioButton
			// 
			this.batchLearnRadioButton.Location = new System.Drawing.Point(312, 392);
			this.batchLearnRadioButton.Name = "batchLearnRadioButton";
			this.batchLearnRadioButton.Size = new System.Drawing.Size(96, 16);
			this.batchLearnRadioButton.TabIndex = 24;
			this.batchLearnRadioButton.Text = "Batch";
			// 
			// openFileDialogPattern
			// 
			this.openFileDialogPattern.DefaultExt = "pat";
			this.openFileDialogPattern.Filter = "Neural Network Pattern Files (*.pat)|*.pat";
			this.openFileDialogPattern.InitialDirectory = "examples/pat";
			this.openFileDialogPattern.RestoreDirectory = true;
			this.openFileDialogPattern.Title = "Load a Neural Network Pattern File...";
			// 
			// saveFileDialogPattern
			// 
			this.saveFileDialogPattern.DefaultExt = "pat";
			this.saveFileDialogPattern.Filter = "Neural Network Pattern Files (*.pat)|*.pat";
			this.saveFileDialogPattern.InitialDirectory = "examples/pat";
			this.saveFileDialogPattern.RestoreDirectory = true;
			this.saveFileDialogPattern.Title = "Save Patterns as a the Neural Network Pattern File...";
			// 
			// fontDialog
			// 
			this.fontDialog.Font = new System.Drawing.Font("Microsoft Sans Serif", 6.5F);
			this.fontDialog.ShowEffects = false;
			// 
			// GUI
			// 
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(952, 637);
			this.Controls.Add(this.mainTabCtrl);
			this.Controls.Add(this.statusBar);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Menu = this.mainMenu;
			this.MinimumSize = new System.Drawing.Size(960, 671);
			this.Name = "GUI";
			this.Text = ".: NN Simulator GUI :.";
			this.Resize += new System.EventHandler(this.GUI_Resize);
			this.Closing += new System.ComponentModel.CancelEventHandler(this.GUI_Closing);
			((System.ComponentModel.ISupportInitialize)(this.infoSBP)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.progressSBP)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.percentSBP)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.timeSBP)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.remainingSBP)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.memUse)).EndInit();
			this.mainTabCtrl.ResumeLayout(false);
			this.designPage.ResumeLayout(false);
			this.groupBox2.ResumeLayout(false);
			this.groupBox1.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.rangeMaxUpDown)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.rangeMinUpDown)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.seedUpDown)).EndInit();
			this.patternPage.ResumeLayout(false);
			this.patternModeGroupBox.ResumeLayout(false);
			this.manualModePanel.ResumeLayout(false);
			this.top10_GroupBox.ResumeLayout(false);
			this.ocrModePanel.ResumeLayout(false);
			this.imgSepGroupBox.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.imgSepThreshHoriznUpDown)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.imgSepThreshVertnUpDown)).EndInit();
			this.sepImgGroupBox.ResumeLayout(false);
			this.windowgroupBox.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.heightnUpDown)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.widthnUpDown)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.ynUpDown)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.xnUpDown)).EndInit();
			this.scaleGroupBox.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.heightScalenUpDown)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.widthScalenUpDown)).EndInit();
			this.groupBox5.ResumeLayout(false);
			this.cannyGroupBox.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.highThreshnUpDown)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.lowThreshnUpDown)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.sigmanUpDown)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.binUpDown)).EndInit();
			this.segmRgnGroupBox.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.heightCharRgnnUpDown)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.widthCharRgnnUpDown)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.yCharRgnnUpDown)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.xCharRgnnUpDown)).EndInit();
			this.featureExtractGroupBox.ResumeLayout(false);
			this.counterGroupBox.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.dyUpDown)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.dxUpDown)).EndInit();
			this.segmGroupBox.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.segmUpDown)).EndInit();
			this.groupBox3.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.noisyImagesNumUpDown)).EndInit();
			this.trainPage.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.maxErrorUpDown)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.cyclesUpDown)).EndInit();
			this.ResumeLayout(false);

		}
		#endregion

		#region Threadsteuerung + Threadstart Methoden
		
		private void calcImageFilters()
		{	
			try
			{
				lock(this)
				{
					// changed
					setFilterBtnState(ThreadStates.START);
					timeSBP.Text = "";
					// t-bä
					this.remainingSBP.Text = "";
					progressPercent = 0;
					statusBar.Invalidate();
					startTime = DateTime.Now;
		            
					filteredImgPB.Image = null;

					Rectangle rect = new Rectangle((int)xCharRgnnUpDown.Value, (int)yCharRgnnUpDown.Value,
						(int)widthCharRgnnUpDown.Value, (int)heightCharRgnnUpDown.Value);
					filteredBmp = new Bitmap(rect.Width, rect.Height);
					srcBmp.SetResolution(filteredBmp.HorizontalResolution, filteredBmp.VerticalResolution);
					Graphics gfx = Graphics.FromImage(filteredBmp);
					gfx.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
					gfx.DrawImage(srcBmp, 0, 0, rect, GraphicsUnit.Pixel);

					if(onGrayCheckBox.Checked)
					{
						this.infoSBP.Text = "Computing gray conversion...";
						filteredBmp = imgFilters[0].compute(filteredBmp);
						filteredImgPB.Image = filteredBmp;
					}
					if(onBrightnCheckBox.Checked)
					{
						this.infoSBP.Text = "Computing brightness normalization...";
						filteredBmp = imgFilters[1].compute(filteredBmp);
						filteredImgPB.Image = filteredBmp;
					}
					if(onHistCheckBox.Checked)
					{
						this.infoSBP.Text = "Computing histogramm equalization...";
						filteredBmp = imgFilters[2].compute(filteredBmp);
						filteredImgPB.Image = filteredBmp;
					}
					if(onBinCheckBox.Checked)
					{
						this.infoSBP.Text = "Computing binary black/white conversion...";
						(imgFilters[5] as BinarizeFilter).thresholdProp = (float)binUpDown.Value;
						filteredBmp = imgFilters[5].compute(filteredBmp);
						filteredImgPB.Image = filteredBmp;
					}
					try
					{
						if(onGaussCheckBox.Checked)
						{
							this.infoSBP.Text = "Computing Gaussian smoothing...";
							(imgFilters[3] as GaussFilter).sigmaProp = (double)sigmanUpDown.Value;
							(imgFilters[3] as GaussFilter).computeGaussMask();
							filteredBmp = imgFilters[3].compute(filteredBmp);
							filteredImgPB.Image = filteredBmp;
						}
						if(onCannyCheckBox.Checked)
						{	
							this.infoSBP.Text = "Computing Canny edge extraction...";						
							(imgFilters[4] as CannyFilter).lowThresholdProp = (int)lowThreshnUpDown.Value;
							(imgFilters[4] as CannyFilter).highThresholdProp = (int)highThreshnUpDown.Value;
							filteredBmp = imgFilters[4].compute(filteredBmp);
							filteredImgPB.Image = filteredBmp;
						}
					}
					catch(FormatException exc)
					{
						MessageBox.Show(exc.Message, "Error");
					}
					filteredImgPB.Image = filteredBmp;

					this.infoSBP.Text = "Separating image...";	
					imgSeparator.LineThreshold = (float)imgSepThreshHoriznUpDown.Value;
					imgSeparator.ColumnThreshold = (float)imgSepThreshVertnUpDown.Value;
					charBmpsAL = new ArrayList(imgSeparator.separateToBitmaps(filteredBmp));
					charsCount = charBmpsAL.Count;
					charsBmpUpDown.Items.Clear();

					gfx = Graphics.FromImage(charPB.Image);
					gfx.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;

					int i;
					for(i=0; i<charsCount; i++)
					{       
						charsBmpUpDown.Items.Add(i+1);
						gfx.DrawImage((Image)charBmpsAL[i], 0, 0, charPB.Width, charPB.Height);
						charPB.Refresh();
					}
					charsBmpUpDown.SelectedIndex = i-1;
					setCtrlsToBmp((Bitmap)charBmpsAL[i-1], false);

					gfx = Graphics.FromImage(filteredImgPB.Image);
					gfx.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
					gfx.DrawRectangles(pen, imgSeparator.Rectangles);
					filteredImgPB.Refresh();

					isDirty = true;
					infoSBP.Text = "All done!";
					calcProgressBar(99, 100);
					// changed
					setExtractButtonState(ThreadStates.STOP);
					delImgBtn.Enabled = true;
				}
			}
			catch(ThreadAbortException) {}
			catch(ThreadStateException) {}
			catch(Exception exc)
			{
				handleException(exc);
				// changed
				setFilterBtnState(ThreadStates.STOP);
				return;
			}
			finally
			{
				// changed
				setFilterBtnState(ThreadStates.STOP);
			}
		}

		private void calcOCRImages()
		{
			try
			{
				lock(this)
				{
					// changed
					setFilterBtnState(ThreadStates.START);
					timeSBP.Text = "";
					// t-bä
					this.remainingSBP.Text = "";
					progressPercent = 0;
					statusBar.Invalidate();
					startTime = DateTime.Now;
					if(this.letterRangeMinUpDown.Text.CompareTo(this.letterRangeMaxUpDown.Text) > 0)
					{
						string txt = this.letterRangeMinUpDown.Text;
						this.letterRangeMinUpDown.Text = this.letterRangeMaxUpDown.Text;
						this.letterRangeMaxUpDown.Text = txt;
					}

					byte firstChar = (byte)this.letterRangeMinUpDown.Text[0];
					byte lastChar = (byte)this.letterRangeMaxUpDown.Text[0];
					charsCount = lastChar - firstChar+1;
					int noiseCount = (int)this.noisyImagesNumUpDown.Value;
					float noiseVal = this.noiseAdjuster.valueProp;

					OCRImageCreator creator;
					HistogramSeparator sep = new HistogramSeparator();
					sep.ColumnThreshold = (float)imgSepThreshVertnUpDown.Value;
					sep.LineThreshold = (float)imgSepThreshHoriznUpDown.Value;
					Bitmap[] bmps;

					charBmpsAL = new ArrayList(charsCount*noiseCount);
					charsBmpUpDown.Items.Clear();
					int index = 0;
					Size biggestBmpSize = new Size(1,1);
					infoSBP.Text = "Generating images...";

					Graphics gfx = Graphics.FromImage(charPB.Image);
					gfx.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;

					for(int i=0; i<charsCount; i++)
					{				
						for(int j=0; j<noiseCount; j++)
						{
							index = i*noiseCount+j;
							creator = new OCRImageCreator(Convert.ToChar(firstChar+i), fontDialog.Font);
							creator.addNoiseToImg(noiseVal); 

							bmps = sep.separateToBitmaps(creator.bmpProp); 

							charsBmpUpDown.Items.Add(index+1);

							charBmpsAL.Add(bmps[0]);
							gfx.DrawImage(bmps[0], 0, 0, charPB.Width, charPB.Height);
							charPB.Refresh();

							if(biggestBmpSize.Width < bmps[0].Width)
								biggestBmpSize.Width = bmps[0].Width;
							if(biggestBmpSize.Height < bmps[0].Height)
								biggestBmpSize.Height = bmps[0].Height;
						}
						if(this.onOCRCharComputing != null)
							onOCRCharComputing(i, charsCount);
					}
					
					infoSBP.Text = (index+1) + " images created!";
					this.calcProgressBar(99, 100);
					setCtrlsToBmp(new Bitmap((Image)charBmpsAL[index], biggestBmpSize), true);
					if(charsBmpUpDown.SelectedIndex == index)
						charsBmpUpDown.SelectedIndex = index-1;
					charsBmpUpDown.SelectedIndex = index;
					isDirty = true;
					// changed
					setExtractButtonState(ThreadStates.STOP);
					delImgBtn.Enabled = false;
				}
			}
			catch(ThreadAbortException) {}
			catch(ThreadStateException) {}
			catch(Exception exc)
			{
				handleException(exc);
				// changed
				setFilterBtnState(ThreadStates.STOP);
				return;
			}
			finally
			{
				// changed
				setFilterBtnState(ThreadStates.STOP);
			}
		}
		private void extractImages()
		{
			try
			{
				lock(this)
				{
					// changed
					setExtractButtonState(ThreadStates.START);
					if(charBmpsAL == null)
					{
						MessageBox.Show("No images to extract!");
						return;
					}
					timeSBP.Text = "";
					// t-bä
					this.remainingSBP.Text = "";
					progressPercent = 0;
					statusBar.Invalidate();
					startTime = DateTime.Now;
	
					float[][] input = new float[charBmpsAL.Count][];

					float[][] output = null;
					if(creationRadioBtn.Checked)
						output = new float[charBmpsAL.Count][];
				
					Assembly asm = Assembly.LoadWithPartialName(OCR_PREPROC_ASMNAME);
					Type t = asm.GetType(this.extractFuncs[this.featureExtraktioncomboBox.SelectedIndex,1], true, true);
					OCRImageExtractor xtractor = (OCRImageExtractor)Activator.CreateInstance(t);
					Rectangle winRect = new Rectangle((int)xnUpDown.Value, (int)ynUpDown.Value, 
						(int)widthnUpDown.Value, (int)heightnUpDown.Value);

					//r-s_2.10.
					if(t.BaseType == typeof(OCRImgExtCounter))
					{
						(xtractor as OCRImgExtCounter).dxProp = (int)dxUpDown.Value;
						(xtractor as OCRImgExtCounter).dyProp = (int)dyUpDown.Value;
					}
					else if(t == typeof(ImgSegmenter))
						(xtractor as ImgSegmenter).nSegmentsProp = (int)segmUpDown.Value;
					
					infoSBP.Text = "Extracting features from images...";

					Bitmap scaledBmp;
					int scaleWidth = (int)widthScalenUpDown.Value;
					int scaleHeight = (int)heightScalenUpDown.Value;
					float scaleFactorW = (float)scaleWidth / (charPB.Image.Width / charPBSx);
					float scaleFactorH = (float)scaleHeight / (charPB.Image.Height / charPBSy);
					winRect.Width = (int)Math.Round(winRect.Width * scaleFactorW, 0);
					winRect.Height = (int)Math.Round(winRect.Height * scaleFactorH, 0);

					int noiseCount = 1;
					if(creationRadioBtn.Checked)
						noiseCount = charBmpsAL.Count/charsCount;
					int index = 0;

					for(int i=0; i<charsCount; i++)
					{				
						for(int j=0; j<noiseCount; j++)
						{
							index = i*noiseCount+j;
							scaledBmp = new Bitmap(scaleWidth, scaleHeight);
							Graphics sgfx = Graphics.FromImage(scaledBmp);
							sgfx.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
							sgfx.DrawImage((Image)charBmpsAL[index], 0, 0, scaleWidth, scaleHeight);

							input[index] = xtractor.computeFeatureVec(scaledBmp, winRect);

							if(output != null)
							{
								output[index] = new float[charsCount];
								if(nn.actFuncForEachNeuronProp[0][0] is TanhActFunc)
									for(int k=0; k<output[index].Length; k++)
										output[index][k] = -1f;
								output[index][i] = 1f;
							}
							if(this.onOCRCharComputing != null)
								onOCRCharComputing(index, charBmpsAL.Count);
						}	
					}
					float[][][] weight = nn.weightNetProp;

					if(this.generateNetcheckBox.Enabled && this.generateNetcheckBox.Checked)
					{
						LearningAlgo la = nn.learnAlgoProp;
						IActivationFunction[][] actfunc = nn.actFuncForEachNeuronProp;
						string netName = nn.netNameProp;
						int[] layers = new int[nn.layersProp.Length-1];
						for(int i=0; i<layers.Length; i++)
							layers[i] = nn.layersProp[i].Length;
						this.nn = new NeuralNet(input[0], layers, output[0].Length, la);
						this.nn.netNameProp = netName;
						nn.actFuncForEachNeuronProp = this.getArrayWithActFuncForType(actfunc[0][0].GetType());
					}
					float[][] oldInput = nn.learnAlgoProp.inputProp;
					try
					{
						float[][] inpAll, outAll;
						if(nn.learnAlgoProp.inputProp != null)
						{
							if(input[0].Length == nn.inputProp.Length)
							{
								if(addPatsCheckBox.Checked)
								{
									inpAll = new float[input.Length+nn.learnAlgoProp.inputProp.Length][];
									nn.learnAlgoProp.inputProp.CopyTo(inpAll, 0);
									input.CopyTo(inpAll, nn.learnAlgoProp.inputProp.Length);
									nn.learnAlgoProp.inputProp = inpAll;
								}
								else
									nn.learnAlgoProp.inputProp = input;
							}
							else
								throw new FormatException();
						}
						else if(this.generateNetcheckBox.Checked)
							nn.learnAlgoProp.inputProp = input;
						else if(nn.inputProp.Length == input[0].Length)
							nn.learnAlgoProp.inputProp = input;
						else						
							throw new FormatException();
						if(nn.learnAlgoProp.teachOutputProp != null && output != null)
						{
							if(output[0].Length == nn.outputLayerProp.Length)
							{
								if(addPatsCheckBox.Checked)
								{
									outAll = new float[output.Length+nn.learnAlgoProp.teachOutputProp.Length][];
									nn.learnAlgoProp.teachOutputProp.CopyTo(outAll, 0);
									output.CopyTo(outAll, nn.learnAlgoProp.teachOutputProp.Length);
									nn.learnAlgoProp.teachOutputProp = outAll;
								}
								else
									nn.learnAlgoProp.teachOutputProp = output;
							}
							else
								throw new FormatException();
						}	
						else if(this.generateNetcheckBox.Checked)
							nn.learnAlgoProp.teachOutputProp = output;
						else if(nn.learnAlgoProp.teachOutputProp != null && output == null)
						{
						/*	if(nn.learnAlgoProp.teachOutputProp.Length >= input.Length)
							{
								noiseCount = nn.learnAlgoProp.teachOutputProp.Length/charsCount;
								output = new float[nn.learnAlgoProp.teachOutputProp.Length/noiseCount][];
								for(int i=0, j=0; i<nn.learnAlgoProp.teachOutputProp.Length; i+=noiseCount, j++)
									output[j] = nn.learnAlgoProp.teachOutputProp[i];
							}
							else
							{
								output = new float[input.Length][];
								int i;
								for(i=0; i<nn.learnAlgoProp.teachOutputProp.Length; i++)
									output[i] = nn.learnAlgoProp.teachOutputProp[i];
								for(; i<output.Length; i++)
									output[i] = new float[nn.outputLayerProp.Length];
							}
						*/
							output = new float[input.Length][];
							for(int i=0; i<output.Length; i++)
								output[i] = new float[nn.outputLayerProp.Length];
							
							if(addPatsCheckBox.Checked)
							{
								outAll = new float[output.Length+nn.learnAlgoProp.teachOutputProp.Length][];
								nn.learnAlgoProp.teachOutputProp.CopyTo(outAll, 0);
								output.CopyTo(outAll, nn.learnAlgoProp.teachOutputProp.Length);
								nn.learnAlgoProp.teachOutputProp = outAll;
							}
							else
								nn.learnAlgoProp.teachOutputProp = output;
						}
//						else if(nn.outputLayerProp.Length == output[0].Length)
//							nn.learnAlgoProp.teachOutputProp = output;
						else						
							throw new FormatException();

					}
					catch(FormatException)
					{
						MessageBox.Show("Generated patterns don't fit to the current networktopology!\n" +
										"-> The new generated patterns were discarded!\n" +
										"Check the \"Scale to:\" values and the extraction method, if they match the current network.",
										"False networktopology!");
						nn.learnAlgoProp.inputProp = oldInput;
						setExtractButtonState(ThreadStates.STOP);
					}
					try{ nn.weightNetProp = weight; }
					catch(ArgumentException) {}
					infoSBP.Text = charBmpsAL.Count + " images extracted!";
					this.calcProgressBar(99, 100);
					isDirty = true;
				}
			}
			catch(ThreadAbortException) {}
			catch(ThreadStateException) {}
			catch(Exception exc)
			{
				handleException(exc);
				// changed
				setExtractButtonState(ThreadStates.STOP);
				return;
			}
			finally
			{
				// changed
				setExtractButtonState(ThreadStates.STOP);
			}
		}
		private void calcNetLearning()
		{	
			try
			{
				lock(this)
				{
					timeSBP.Text = "";
					// t-bä
					this.remainingSBP.Text = "";
					progressPercent = 0;
					statusBar.Invalidate();
					this.learnAlgoComboBox.Enabled = false;
					startTime = DateTime.Now;

					this.errorListView.Items.Clear();
					xValues = new ArrayList();
					fxValues = new ArrayList();	
					this.errorMax = 0;

					if(fastModeCheckBox.Checked)
						errorListView.BeginUpdate();
					if(nn != null)
					{
						isDirty = true;
						this.infoSBP.Text = "Learning...";
						if(isBackpropBatchMethod)
							(nn.learnAlgoProp as Backpropagation).learnPatternsBatch();
						else
							nn.learnAlgoProp.learnPatterns();
			
						if(nn.learnAlgoProp.iterationProp == nn.learnAlgoProp.maxIterationProp)
							this.infoSBP.Text = "Problem could not be learned in " 
								+ nn.learnAlgoProp.maxIterationProp.ToString() + " cycles! :-(";
						else
							this.infoSBP.Text = "Finished learning after " + (nn.learnAlgoProp.iterationProp+1).ToString() + " cycles."
								+ "Error: " + nn.learnAlgoProp.errorProp;

						calcProgressBar(99, 100);
						errorListView.EndUpdate();
						this.currentErrorTxt.Text = "current global Error : " + nn.learnAlgoProp.errorProp.ToString("0.000000");
						showElapsedTime();
						errorPB.Invalidate();
						this.learnAlgoComboBox.Enabled = true;
					}
				}
			}
			catch(ThreadAbortException)			
			{
				//r-s Begin
				if(threadStopFromLearnAutomation)
				{
					threadStopFromLearnAutomation = false;
					nn.randomizeWeights();
					restartThread(new ThreadStart(calcNetLearning));
				}
				//r-s End
			}
			catch(ThreadStateException) {}
			catch(Exception exc)
			{
				handleException(exc);
				return;
			}
		}

		private void restartThread(ThreadStart calledMethod)
		{
			try
			{
				stopThread();
				calcThread = new Thread(calledMethod);
				// t-bä
				// because of 100%-usage of the Processor-Power
				// necessary to do something else while learning (better working)
				calcThread.Priority = ThreadPriority.BelowNormal;
				calcThread.Start();
			}
			catch(ThreadAbortException) {}
			catch(ThreadStateException) {}
		}

		private void stopThread()
		{
			try
			{ 
				if(calcThread != null) 
					calcThread.Abort(); 
			}
			catch(ThreadAbortException) {}
			catch(ThreadStateException) {}
		}

		#endregion

		#region Misc. methods

		private void updateAxisText()
		{
			xAxisText = "-> cycles | max: " + nn.learnAlgoProp.maxIterationProp;
			yAxisText = "^ error | max: " + this.errorMax;
		}

		private void extractFilenameAndSetTitle(string absolutePath, bool setShortPath)
		{
			string fileName = absolutePath;
			if(setShortPath)
				fileName = fileName.Substring(fileName.LastIndexOf('\\')+1);
			//r-s
			this.Text = formTitle + " ~ " + nn.netNameProp + " ~ " + fileName;
		}

		private void loadNet(string filename)
		{
			//			if(checkDirtyState() == DialogResult.Cancel && nn != null)
			//				return;
			removeLearnAlgoParamCtrls();
			isBackpropBatchMethod = false;
			nn = FileManager.getInstance().readNetworkFromXml(filename);
			nn.learnAlgoProp.onErrorCalculated += visualizeErrorProgress;
			//r-s
//			nn.learnAlgoProp.onErrorCalculated += checkLastErrorsAndCtrlLearning;
			string learnAlgoStr = nn.learnAlgoProp.ToString();
			learnAlgoStr = learnAlgoStr.Substring(learnAlgoStr.LastIndexOf('.')+1);	
			learnAlgoStr = learnAlgoStr.Substring(0, 7);
			learnAlgoComboBox.SelectedIndex = learnAlgoComboBox.FindString(learnAlgoStr);
			updateNNParamCtrls();
			createLearnAlgoParamCtrls();
			this.showNeuralNet();
			this.drawNetTopology();
			isDirty = false;
		}

		private void loadNetAndSetCtrls(string fileName)
		{
			loadNet(fileName);
			extractFilenameAndSetTitle(fileName, true);
			this.setActFuncComboBoxToCurrentActFunc();
			if(this.mainTabCtrl.SelectedTab == this.patternPage)
			{
				this.showNeuralNet();
				this.drawNetTopology();
			}
		}
		private void updateNNParamCtrls()
		{
			//r-s-21.9.
			if(nn.actFuncForEachNeuronProp[0][0].GetType().BaseType == typeof(SigmoidActFunc))
				steepnessAdjuster.valueProp = steepnessOfAllSigmoidNeuronsProp;			
		}

		//r-s
		private void associateLearnAlgosWithNet(NeuralNet nn)
		{
			this.learningAlgos = new LearningAlgo[]
			{
				new Backpropagation(nn),
				new Backpropagation(nn),
				new GeneticLearningAlgorithm(nn)
			};

			for(int i=0; i<this.learningAlgos.Length; i++)
			{
				learningAlgos[i].onErrorCalculated += visualizeErrorProgress;
//				learningAlgos[i].onErrorCalculated += checkLastErrorsAndCtrlLearning;

				learningAlgos[i].inputProp = nn.learnAlgoProp.inputProp;
				learningAlgos[i].teachOutputProp = nn.learnAlgoProp.teachOutputProp;
			}
			// t-bä was here
			/*			backPropLearnAlgo = new Backpropagation(nn);
						backPropLearnAlgo.inputProp = nn.learnAlgoProp.inputProp;
						backPropLearnAlgo.teachOutputProp = nn.learnAlgoProp.teachOutputProp;
						(backPropLearnAlgo as Backpropagation).onErrorCalculated += visualizeErrorProgress;
						//r-s
						(backPropLearnAlgo as Backpropagation).onErrorCalculated += checkLastErrorsAndCtrlLearning;

						geneticLearnAlgo = new GeneticLearningAlgorithm(nn);
						geneticLearnAlgo.inputProp = nn.learnAlgoProp.inputProp;
						geneticLearnAlgo.teachOutputProp = nn.learnAlgoProp.teachOutputProp;
						geneticLearnAlgo.onErrorCalculated += visualizeErrorProgress;
						// t-bä
						//	(geneticLearnAlgo as GeneticLearningAlgorithm).onErrorCalculated += displayGeneration;
						//r-s
						geneticLearnAlgo.onErrorCalculated += checkLastErrorsAndCtrlLearning;
			*/		
		}

		private void showElapsedTime()
		{
			TimeSpan t = DateTime.Now - startTime;
			timeSBP.Text = String.Format("Time elapsed: {0:00}:{1:00}:{2:00}:{3:000}\n", t.Hours, t.Minutes, t.Seconds, t.Milliseconds);
		}

		private void showRemainingTime(int current, int total)
		{
			TimeSpan t = DateTime.Now - startTime;
			// t-bä
			// shows the approximate(?) remaining time
			TimeSpan rem;
			if(current > 0)
				rem = new TimeSpan((t.Ticks/current)*(total-current));
			else
				rem = new TimeSpan(t.Ticks*(total-current));
			remainingSBP.Text = String.Format("remaining: {0:00}:{1:00}:{2:00}:{3:000}",rem.Hours, rem.Minutes, rem.Seconds, rem.Milliseconds);
		}

		//r-s-21.9.
		private IActivationFunction[][] getArrayWithActFuncForType(Type type)
		{
			IActivationFunction[][] actFuncs = new IActivationFunction[nn.layersProp.Length][];
			for(int i=0; i<nn.layersProp.Length; i++)
			{
				actFuncs[i] = new IActivationFunction[nn.layersProp[i].neuronsProp.Length];
				for(int j=0; j<nn.layersProp[i].neuronsProp.Length; j++)
				{
					actFuncs[i][j] = (IActivationFunction)Activator.CreateInstance(type);
				}
			}
			return actFuncs;
		}

		private void mainTabCtrl_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			if((sender as TabControl).SelectedTab == this.designPage)
				showNetBtn.PerformClick();
			else if((sender as TabControl).SelectedTab == this.patternPage)
			{
				if(this.manualModeRadioBtn.Checked)
					this.updatePatternTreeView();
				else if(this.ocrModeRadioBtn.Checked)
					setCharRgnScaleCtrls(srcBmp, false);
			}
		}
		private static void handleException(Exception exc)
		{
			try
			{
				FileManager.getInstance().writeSystemInfosToErrorLogFile();
				FileManager.getInstance().writeExceptionToErrorLogFile(exc);
			}
			catch(Exception ex)
			{
				MessageBox.Show("Problem writing error log file! \n"
					+ "Exceptionmessage:\n"
					+ ex.Message);
				return;
			}
		
			try
			{
				Application.Run(new ErrorMessageBox(exc));
			}
			catch(Exception ex)
			{
				MessageBox.Show("Problem showing error MessageBox! \n"
					+ "Exceptionmessage:\n"
					+ ex.Message);
				return;
			}
		}

		private DialogResult checkDirtyState()
		{
			DialogResult r = DialogResult.OK;
			if(isDirty)
			{
				r = MessageBox.Show("Do you want to save your changes?", "Save changes?", MessageBoxButtons.YesNoCancel);
				if(r == DialogResult.Yes)
				{
					this.fileMenuSaveItem.PerformClick();
					isDirty = false;
				}
			}

			return r;
		}

		private void setCtrlsToBmp(Bitmap bmp, bool setScaling)
		{
			charPB.Image = new Bitmap(charPB.Width, charPB.Height);
			Graphics gfx = Graphics.FromImage(charPB.Image);
			gfx.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
			gfx.Clear(Color.White);
			gfx.DrawImage(bmp, 0, 0, charPB.Image.Width, charPB.Image.Height);

			charPBSx = (float)charPB.Width / bmp.Width;
			charPBSy = (float)charPB.Height / bmp.Height;
			xnUpDown.Value = 0;
			ynUpDown.Value = 0;
			widthnUpDown.Value = (decimal)bmp.Width;
			heightnUpDown.Value = (decimal)bmp.Height;
			if(setScaling)
			{
				widthScalenUpDown.Value = widthnUpDown.Value;
				heightScalenUpDown.Value = heightnUpDown.Value;
			}
			charPB.Refresh();
		}

		public void setCharRgnScaleCtrls(Bitmap srcBmp, bool doSetCtrls)
		{
			if(srcBmp == null || srcImgPB == null)
				return;
			if(srcImgPB.Width <= 0 || srcImgPB.Height <= 0)
				return;

			srcImgPB.Image = new Bitmap(srcImgPB.Width, srcImgPB.Height);
			Graphics gfx = Graphics.FromImage(srcImgPB.Image);
			gfx.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
			gfx.DrawImage(srcBmp, 0, 0, srcImgPB.Width, srcImgPB.Height);

			charRgnSx = (float)srcImgPB.Width / srcBmp.Width;
			charRgnSy = (float)srcImgPB.Height / srcBmp.Height;
			if(doSetCtrls)
			{
				xCharRgnnUpDown.Value = 0;
				yCharRgnnUpDown.Value = 0;
				widthCharRgnnUpDown.Value = (decimal)srcBmp.Width;
				heightCharRgnnUpDown.Value = (decimal)srcBmp.Height;
			}
			srcImgPB.Refresh();
		}

		#endregion

		#region Eventhandler calcXXX

		private void calcProgressBar(int current, int total)
		{	
			lock(this)
			{
				progressPercent = (int)(((float)current / total)*100);
				percentSBP.Text = (progressPercent + 1).ToString() + " %";
				if(!fastModeCheckBox.Checked)
				{
					showElapsedTime();
					showRemainingTime(current, total);
					statusBar.Invalidate();
				}
			}
		}

		private void calcErrorGraph(float error)
		{	
			lock(this)
			{
				if(error > errorMax)
				{
					errorMax = error;
					updateAxisText();
					calcScaleFactors();
				}				
				xValues.Add((float)nn.learnAlgoProp.iterationProp);
				fxValues.Add(error);
				//		ListViewItem lvi = new ListViewItem(String.Format("{0,3}", (nn.learnAlgoProp.iterationProp+1)));
				ListViewItem lvi = new ListViewItem((nn.learnAlgoProp.iterationProp+1).ToString("000"));
				// t-bä
				// lvi.SubItems.Add(error.ToString());
				lvi.SubItems.Add(error.ToString("0.000000"));
				this.errorListView.Items.Add(lvi);
				calcProgressBar(nn.learnAlgoProp.iterationProp, nn.learnAlgoProp.maxIterationProp);
				if(!fastModeCheckBox.Checked)
				{
					// t-bä
					this.currentErrorTxt.Text = "current global Error : " + error.ToString("0.000000");
					errorPB.Invalidate();
				}
				Thread.Sleep((int)this.slowmoAdjuster.valueProp);
			}
		}

		//t-bä
/*		private void showGeneration(float error)
		{
			lock(this)
			{
				if(nn.learnAlgoProp is GeneticLearningAlgorithm)
				{
					GeneticLearningAlgorithm algo = nn.learnAlgoProp as GeneticLearningAlgorithm;
					TreeNode dad = new TreeNode("Generation: " + algo.gernerationCountProp.ToString("000") + " | minError: " + error.ToString("0.00000"));
					int n=1;
					foreach(Individual i in algo.CurrentGeneration)
					{
						TreeNode child = new TreeNode("Individual " + n.ToString("000") + " | Error: " + i.Error.ToString("00.000000") + " | Fitness: " + i.Fitness.ToString("0.00000"));
						for(int j=0; j<i.Parameter.Length; j++)
						{
							TreeNode childSub1 = new TreeNode("Networklayer " + j.ToString());
							for(int k=0; k<i.Parameter[j].Length; k++)
							{
								TreeNode childSub2 = new TreeNode("Neuron " + k.ToString());
								for(int l=0; l<i.Parameter[j][k].Length; l++)
								{
									childSub2.Nodes.Add(i.Parameter[j][k][l].ToString());
								}
								childSub1.Nodes.Add(childSub2);
							}
							child.Nodes.Add(childSub1);
						}
						dad.Nodes.Add(child);
						n++;
					}
					netTreeView.Nodes.Add(dad);
				}
			}
		}
*/
		//r-s
		private void automatedLearning(float error)
		{
			if(lastErrors.Count == QUEUE_SIZE)
				lastErrors.Dequeue();
			lastErrors.Enqueue((float)Math.Round(error, 3));
			float prevErr = (float)lastErrors.Peek();
			int sameErrs = 0;
			foreach(float err in lastErrors)
			{				
				if(prevErr != err)
					break;
				else
					sameErrs++;
				prevErr = err;
			}
			if(sameErrs == QUEUE_SIZE)
			{
				threadStopFromLearnAutomation = true;
				stopThread();
			}
		}

		private void calcScaleFactors()
		{	
			if(nn != null)
			{
				sx = (float)errorPB.Width / nn.learnAlgoProp.maxIterationProp;
				sy = (float)errorPB.Height / errorMax;
			}
		}

		#endregion

		#region Various form event handlers

		private void statusBar_DrawItem(object sender, System.Windows.Forms.StatusBarDrawItemEventArgs sbdevent)
		{
			sbdevent.Graphics.FillRectangle(new SolidBrush(Color.Blue), sbdevent.Bounds.X, sbdevent.Bounds.Y,
				progressPercent, sbdevent.Bounds.Height);
		}

		private void GUI_Resize(object sender, System.EventArgs ev)
		{
			calcScaleFactors();
			setCharRgnScaleCtrls(srcBmp, false);
		}
		
		private void GUI_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			if(checkDirtyState() == DialogResult.Cancel)
			{
				e.Cancel = true;
				return;
			}

			stopThread();
		}

		private void errorPB_Paint(object sender, System.Windows.Forms.PaintEventArgs pea)
		{
			if(xValues != null && fxValues != null)
			{
				if(Single.IsNaN(nn.learnAlgoProp.errorProp))
				{
					pea.Graphics.DrawString("Undefined value for error! Divison with 0! Randomize weight and start again!", 
						axisFont, axisBrush, errorPB.Width/4, errorPB.Height/2);
					this.stopThread();
					return;
				}
				else if(Single.IsPositiveInfinity(nn.learnAlgoProp.errorProp))
				{
					pea.Graphics.DrawString("Value for error is positive infinity! Randomize weight and start again!", 
						axisFont, axisBrush, errorPB.Width/4, errorPB.Height/2);
					this.stopThread();
					return;
				}
				else if(Single.IsNegativeInfinity(nn.learnAlgoProp.errorProp))
				{
					pea.Graphics.DrawString("Value for error is negative infinity! Randomize weight and start again!", 
						axisFont, axisBrush, errorPB.Width/4, errorPB.Height/2);
					this.stopThread();
					return;
				}
        
				pea.Graphics.TranslateTransform(0, errorPB.Height);
				pea.Graphics.ScaleTransform(1, -1);
				for(int i=1; i<xValues.Count && i<fxValues.Count; i++)
					pea.Graphics.DrawLine(pen, ((float)xValues[i-1])*sx,  ((float)fxValues[i-1])*sy,
						((float)xValues[i])*sx,    ((float)fxValues[i])*sy);
				pea.Graphics.ScaleTransform(1, -1);
				pea.Graphics.TranslateTransform(0, -errorPB.Height);
        
				pea.Graphics.DrawString(yAxisText, axisFont, axisBrush, 0, 0);
				pea.Graphics.DrawString(xAxisText, axisFont, axisBrush, errorPB.Width - xAxisText.Length*5, 
					errorPB.Height - axisFont.Height);
			}
		}

		private void errorPB_MouseMove(object sender, System.Windows.Forms.MouseEventArgs e)
		{
			this.toolTip.SetToolTip((sender as PictureBox), 
				"cycle: " + Math.Round(e.X/sx, 0) + "; error: " + (errorMax-e.Y/sy)); 
		}

		private void t_Tick(Object o, System.EventArgs e)
		{
			GC.Collect();
			memUse.Text = "Memory usage: ";
			memUse.Text += ((float)System.Environment.WorkingSet/(1024*1024)).ToString("0.00 MB");
		}

		#endregion
		
		#region Net-Designer event handlers

		// t-bä
		private void showNetBtn_Click(object sender, System.EventArgs e)
		{
			this.showNeuralNet();
			this.drawNetTopology();
		}

		private void clearBtn_Click(object sender, System.EventArgs e)
		{
			netTreeView.Nodes.Clear();
			//r-s
			netNameTxt.Text = "";
			// changed, unnötigen cast entfernt
			netPictureBox.CreateGraphics().Clear(netPictureBox.BackColor);
		}

		//r-s
		private void netTreeExpand_Btn_Click(object sender, System.EventArgs e)
		{
			netTreeView.ExpandAll();
		}

		//r-s
		private void netTreeCollapse_Btn_Click(object sender, System.EventArgs e)
		{
			netTreeView.CollapseAll();
		}

		// changed namen geändert, da reihenfolge gedreht wurde
		private void insertLayerBeforeBtn_Click(object sender, System.EventArgs e)
		{
			isDirty = true;
			TreeNode current = netTreeView.SelectedNode;
			int selected;

			if(current != null)
			{
				do
				{
					selected = current.Index;
					current = current.Parent;
				}while(current != null);
			}
			else
				selected = netTreeView.Nodes.Count;

			try
			{
				if(int.Parse(this.neuronsNumTxt.Text) > 1000)
					throw new FormatException("Number of Neurons have to be <= 1000");
				
				this.insertNode(selected+1);
					
				this.neuronsNumTxt.Text = "";
			}
			catch(FormatException)
			{
				MessageBox.Show("Please type in a number of neurons!", "Error while adding a network layer");
				return;
			}
		}

		// changed namen geändert, da reihenfolge gedreht wurde
		private void insertLayerAfterBtn_Click(object sender, System.EventArgs e)
		{
			isDirty = true;
			TreeNode current = netTreeView.SelectedNode;
			int selected;
			
			if(current != null)
			{
				do
				{
					selected = current.Index;
					current = current.Parent;
				}while(current != null);
			}
			else
				selected = 0;
			try
			{
				if(int.Parse(this.neuronsNumTxt.Text) > 1000)
					throw new FormatException("Number of Neurons have to be <= 1000");
				
				this.insertNode(selected);
					
				this.neuronsNumTxt.Text = "";
			}
			catch(FormatException)
			{
				MessageBox.Show("Please type in an Number of Neurons!","Error while adding a network layer");
				return;
			}
		}

		private void insertNode(int index)
		{
			TreeNode dad = new TreeNode("Layer " + (index).ToString()
				+ " (" + int.Parse(this.neuronsNumTxt.Text).ToString()
				+ " Neuron" + ((int.Parse(this.neuronsNumTxt.Text)>1)?"s":"") + ")");
				
			for(int i=0; i<int.Parse(this.neuronsNumTxt.Text); i++)
				dad.Nodes.Add("Neuron " + (i+1).ToString());
			
			netTreeView.Nodes.Insert(index,dad);
		}

		private void generateNetBtn_Click(object sender, System.EventArgs e)
		{
			float[][] inputPatterns = null;
			float[][] teachOutputPatterns = null;
			// changed, comment siehe unten
//			float[][][] weight = nn.weightNetProp;

			if(netTreeView.Nodes.Count < 1)
			{
				MessageBox.Show("No net designed", "Error while reading current net");
				return;
			}

			string str = "";
			int [] hidden;

			inputPatterns = nn.learnAlgoProp.inputProp;
			teachOutputPatterns = nn.learnAlgoProp.teachOutputProp;

			if(netTreeView.Nodes.Count>2)
				hidden = new int[netTreeView.Nodes.Count-2];
			else
				hidden = new int[0];
			
			int layerCount = hidden.Length-1;
			for(int i=layerCount; i>=0; i--)
				hidden[layerCount-i] = netTreeView.Nodes[i+1].Nodes.Count;
			
			// create new Neural net
			this.nn = new NeuralNet(
				new float[netTreeView.Nodes[netTreeView.Nodes.Count-1].Nodes.Count], hidden, 
				netTreeView.Nodes[0].Nodes.Count, this.learningAlgos[this.learnAlgoComboBox.SelectedIndex]);			
			
			for(int i=netTreeView.Nodes.Count-1; i>=0; i--)
				str += netTreeView.Nodes[i].Nodes.Count.ToString() + "-";

			str += "Net";			
			nn.netNameProp = str;					
			this.netNameTxt.Text = nn.netNameProp;
			//r-s
			extractFilenameAndSetTitle("", false);
			associateLearnAlgosWithNet(nn);

			nn.learnAlgoProp.inputProp = inputPatterns;
			nn.learnAlgoProp.teachOutputProp = teachOutputPatterns;

			if(inputPatterns != null && teachOutputPatterns != null)
				if(inputPatterns[0].Length != nn.inputProp.Length && teachOutputPatterns[0].Length != nn.outputLayerProp.Length)
					nn.learnAlgoProp.inputProp = nn.learnAlgoProp.teachOutputProp = null;

			if(nn.learnAlgoProp.inputProp != null)
			{
				if(inputPatterns[0].Length == nn.inputProp.Length)
					nn.learnAlgoProp.inputProp = inputPatterns;
				else
				{
					nn.learnAlgoProp.inputProp = new float[nn.learnAlgoProp.teachOutputProp.Length][];
					for(int i=0; i<nn.learnAlgoProp.teachOutputProp.Length; i++)
						nn.learnAlgoProp.inputProp[i] = new float[nn.inputProp.Length];
				}
			}
			if(nn.learnAlgoProp.teachOutputProp != null)
			{
				if(teachOutputPatterns[0].Length == nn.outputLayerProp.Length)
					nn.learnAlgoProp.teachOutputProp = teachOutputPatterns;
				else
				{
					nn.learnAlgoProp.teachOutputProp = new float[nn.learnAlgoProp.inputProp.Length][];
					for(int i=0; i<nn.learnAlgoProp.inputProp.Length; i++)
						nn.learnAlgoProp.teachOutputProp[i] = new float[nn.outputProp.Length];
				}
			}

			this.ActivationFunctionComboBox_SelectedIndexChanged(this.activationFuncComboBox, null);

			// changed
			// wo war der sinn?
//			try{ nn.weightNetProp = weight; }
//			catch(ArgumentException) { }

			this.showNeuralNet();
			this.drawNetTopology();
			isDirty = true;
		}

		private void netPictureBox_Paint(object sender, System.Windows.Forms.PaintEventArgs e)
		{
			//			((PictureBox)sender).BackgroundImage = bmp;
			//			this.showNeuralNet();
			//			this.drawNetTopology();
		}

		private void netPictureBox_Resize(object sender, System.EventArgs e)
		{
			/*	if(((PictureBox)sender).BackgroundImage != bmp)
					this.drawNetTopology();
				else
					((PictureBox)sender).BackgroundImage = bmp;
			*/	this.drawNetTopology();
		}

		// t-bä
		private void showNeuralNet()
		{
			netTreeView.Nodes.Clear();
			nn.propagate();
			TreeNode dad;
		
			for(int i=nn.layersProp.Length-1; i>=0; i--)
			{
				dad = new TreeNode(((i==nn.layersProp.Length-1)?"Output-Layer":("Hidden-Layer " + (i+1).ToString()))
					+ " (" + nn.layersProp[i].Length.ToString()
					+ " Neuron" + ((nn.layersProp[i].Length>1)?"s":"") +")");
				for(int j=0; j<nn.layersProp[i].neuronsProp.Length; j++)
				{
					TreeNode child = new TreeNode("Neuron " + (j+1).ToString() + " (Out: "
						+ nn.layersProp[i].neuronsProp[j].outputProp.ToString("0.0000000") + ")");
					
					Neuron n = nn.layersProp[i].neuronsProp[j];
					IActivationFunction actFunc = n.actFuncProp;
					
					TreeNode actFuncNode = new TreeNode(actFunc.Name);

					for(int k=0; k<actFunc.activationProps.Length; k++)
					{
						actFuncNode.Nodes.Add(new TreeNode(
							actFunc.activationProps[k] + ": " +
							actFunc.GetType().GetProperty(actFunc.activationProps[k]+"Prop").GetValue(actFunc,null).ToString()
							));
					}
					
					TreeNode props = new TreeNode("Properties");
					props.Nodes.Add(actFuncNode);
					props.Nodes.Add(new TreeNode("Threshold: " + n.thresholdProp.ToString("0.0000000")));
					props.Nodes.Add(new TreeNode("Net-Out (∑ w∙i): " + n.netProp.ToString("0.0000000")));

					child.Nodes.Add(props);

					for(int k=0; k<nn.layersProp[i].neuronsProp[j].inputProp.Length; k++)
					{
						child.Nodes.Add(new TreeNode("In: "
							+ nn.layersProp[i].neuronsProp[j].inputProp[k].ToString("0.0000000")
							+ " (Weight: " + nn.layersProp[i].neuronsProp[j].weightProp[k].ToString("0.0000000")
							+ ")"));
					}
					dad.Nodes.Add(child);
				}
				netTreeView.Nodes.Add(dad);
			}

			dad = new TreeNode("Input-Layer (" + nn.inputProp.Length.ToString()
				+ " Neuron" + ((nn.inputProp.Length>1)?"s":"") +")");

			for(int i=0; i<nn.inputProp.Length; i++)
			{
				TreeNode child = new TreeNode("Neuron " + (i+1).ToString() + 
					" (Out: " + nn.inputProp[i].ToString("0.0000000") + ")");
				child.Nodes.Add(new TreeNode("In: " + nn.inputProp[i].ToString("0.0000000") + 
					" (Weight: " + (1.0).ToString("0.0000000") + ")"));
				dad.Nodes.Add(child);
			}
			netTreeView.Nodes.Add(dad);
			this.netNameTxt.Text = nn.netNameProp;
			setActFuncComboBoxToCurrentActFunc();
		}

		private float xstep;
		private float ystep;
		private float diameter;
		private void drawNetTopology()
		{
			if(netPictureBox.Width <= 0 || netPictureBox.Height <= 0)
				return;
			try
			{
				if(this.nn == null)
					throw new Exception("No Network!");
			}
			catch(Exception exc)
			{
				MessageBox.Show(exc.Message, "Error while reading current net");
				return;
			}

			// allocate array for calculating positions
			float[][] points = new float[nn.layersProp.Length+1][];
			points[0] = new float[nn.inputProp.Length];

		
			float range = nn.inputProp.Length;
			float step = range/(nn.inputProp.Length-1);
			if(nn.inputProp.Length == 1)
			{
				range = 0;
				step = 0;
			}

			// save the max horizontal range
			float xmax = range;

			// calculate the input layer positions
			for(int i=0; i<points[0].Length; i++)
				points[0][i] = -range/2+i*step;

			// calculate the other positions
			for(int i=1; i<points.Length; i++)
			{
				points[i] = new float[nn.layersProp[i-1].Length];

				range = nn.layersProp[i-1].Length;
				step = range/(nn.layersProp[i-1].Length-1);

				// save the max horizontal range
				if(range>xmax)
					xmax = range;

				if(nn.layersProp[i-1].Length == 1)
				{
					range = 0;
					step = 0;
				}

				for(int j=0; j<points[i].Length; j++)
					points[i][j] = -range/2+j*step;
			}

			// calculate the spaces between neurons and layers
			xstep = netPictureBox.Width/(xmax+1);
			ystep = (netPictureBox.Height-10)/points.Length;
			diameter = ystep/10;
		
			// transform the view
			Bitmap bmp = new Bitmap(netPictureBox.Width, netPictureBox.Height);
			Graphics grfx = Graphics.FromImage(bmp);
			grfx.Clear(netPictureBox.BackColor);
			grfx.TranslateTransform(netPictureBox.Width/2,netPictureBox.Height/2);
			// rotate
			xstep *= -1; ystep *= -1;

			grfx.DrawString("Input", this.axisFont, this.axisBrush, -netPictureBox.Width/2+10, netPictureBox.Height/2-30);
			grfx.DrawString("Output", this.axisFont, this.axisBrush, -netPictureBox.Width/2+10,-netPictureBox.Height/2+10);

			grfx.TranslateTransform(0,-(float)(points.Length-1)/2*ystep);

			// draw the neurons and lines for the whole network
			for(int i=0; i<points.Length; i++)
				for(int j=0; j<points[i].Length; j++)
				{
					// input
					if(i==0)
						grfx.DrawLine(Pens.CadetBlue,xstep*points[i][j],i*ystep-ystep/2,xstep*points[i][j],i*ystep);
						// output
					else if(i==points.Length-1)
						grfx.DrawLine(Pens.CadetBlue,xstep*points[i][j],i*ystep,xstep*points[i][j],i*ystep+ystep/2);

					// between neurons
					if(i<points.Length-1)
						for(int k=0; k<points[i+1].Length; k++)
							grfx.DrawLine(Pens.CadetBlue,xstep*points[i][j],i*ystep,xstep*points[i+1][k],(i+1)*ystep);
				
					// the neurons
					grfx.FillEllipse(Brushes.Red,xstep*points[i][j]-diameter/2,i*ystep-diameter/2,diameter,diameter);
				}
			//				grfx.DrawImage(bmp,-50,-50);
		netPictureBox.BackgroundImage = bmp;
		}


		// changed, netName wird erst beim verlassen (fokusverlust) gesetzt
		private void netNameTxt_Leave(object sender, System.EventArgs e)
		{
			nn.netNameProp = netNameTxt.Text;
			//	this.Text = ".: NN Simulator GUI :. ~ " + nn.netName;
			//r-s
			extractFilenameAndSetTitle("", false);
		}

		private void netNameTxt_KeyUp(object sender, System.Windows.Forms.KeyEventArgs e)
		{
			if(e.KeyCode == System.Windows.Forms.Keys.Enter)
                netNameTxt_Leave(sender, new System.EventArgs());
		}
		
		private void ActivationFunctionComboBox_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			int selected = ((ComboBox)sender).SelectedIndex;
			nn.actFuncForEachNeuronProp = getArrayWithActFuncForType(actFuncs[selected].GetType());
			
			if(actFuncs[selected].GetType() == actFuncs[2].GetType())
				this.steepnessAdjuster.Visible = false;
			else
				this.steepnessAdjuster.Visible = true;
			
			this.setRandomOptions();
			this.showNeuralNet();
			nn.randomizeWeights();
			isDirty = true;
		}

		private void randomOptionsDefaultBtn_Click(object sender, System.EventArgs e)
		{
			setRandomOptions();
		}

		private void deleteLayerBtn_Click(object sender, System.EventArgs e)
		{
			TreeNode tmp = netTreeView.SelectedNode;
			if(tmp != null)
			{
				int selected;
				do
				{
					selected = tmp.Index;
					tmp = tmp.Parent;
				} while(tmp != null);

				if(DialogResult.Yes == MessageBox.Show("Really delete Layer:\n" + netTreeView.Nodes[selected].Text, "Delete layer?",
					MessageBoxButtons.YesNo,MessageBoxIcon.Question))
				{
					this.netTreeView.Nodes.RemoveAt(selected);
				}
				isDirty = true;
			}
		}
		
		/// <summary>
		/// sets the ComboBox to the current Activation Function of the Net
		/// </summary>
		private void setActFuncComboBoxToCurrentActFunc()
		{
			if(activationFuncComboBox.Items.Count > 0)
			{
				//			if(this.activationFuncComboBox.SelectedIndexChanged != null)
				this.activationFuncComboBox.SelectedIndexChanged -= new System.EventHandler(this.ActivationFunctionComboBox_SelectedIndexChanged);

				for(int i=0; i<actFuncs.Length; i++)
				{
					if(this.actFuncs[i].GetType() == nn.layersProp[0].neuronsProp[0].actFuncProp.GetType())
						this.activationFuncComboBox.SelectedIndex = i;
				}
				// manual entry of the event-handler
				this.activationFuncComboBox.SelectedIndexChanged += new System.EventHandler(this.ActivationFunctionComboBox_SelectedIndexChanged);
			}
		}

		private void randomOptionsCheckBoxs_CheckedChanged(object sender, System.EventArgs e)
		{
			CheckBox cb = (CheckBox)sender;

			if(cb == this.timeAsSeedCheckBox)
			{
				this.useTimeAsSeed = this.timeAsSeedCheckBox.Checked;
				this.seedUpDown.Enabled = !this.timeAsSeedCheckBox.Checked;
			}
			else if(cb == this.optimalRangeCheckBox)
			{
				this.useBestRange = this.optimalRangeCheckBox.Checked;
				this.rangeMinUpDown.Enabled = !this.optimalRangeCheckBox.Checked;
				this.rangeMaxUpDown.Enabled = !this.optimalRangeCheckBox.Checked;
			}

			if(useTimeAsSeed & useBestRange)
				this.randomOptionsDefaultBtn.Enabled = false;
			else
				this.randomOptionsDefaultBtn.Enabled = true;
		}

		
		private void seedUpDown_ValueChanged(object sender, System.EventArgs e)
		{
			this.randSeed = (int)((NumericUpDown)sender).Value;
		}

		private void rangeMinUpDown_ValueChanged(object sender, System.EventArgs e)
		{
			this.rangeMin = (float)((NumericUpDown)sender).Value;
		}

		private void rangeMaxUpDown_ValueChanged(object sender, System.EventArgs e)
		{
			this.rangeMax = (float)((NumericUpDown)sender).Value;
		}

		private void setRandomOptions()
		{
			if(!useTimeAsSeed)
			{
				this.randSeed = this.randSeedDefault;
				this.seedUpDown.Value = (decimal)randSeed;
			}

			if(!useBestRange)
			{
				float range = nn.actFuncForEachNeuronProp[0][0].maxProp - nn.actFuncForEachNeuronProp[0][0].minProp;
				this.rangeMin = nn.actFuncForEachNeuronProp[0][0].minProp + range * 0.25f;
				this.rangeMax = nn.actFuncForEachNeuronProp[0][0].maxProp - range * 0.25f;
			
				this.rangeMinUpDown.Value = (decimal)rangeMin;
				this.rangeMaxUpDown.Value = (decimal)rangeMax;
			}
		}

		private void manualPatternModeTextBox_KeyUp(object sender, System.Windows.Forms.KeyEventArgs e)
		{
			if(e.KeyCode == System.Windows.Forms.Keys.Enter)
			{
				if(sender == this.inputTxt)
				{
					this.changeInputPatternBtn.PerformClick();
				}
				else if(sender == this.outputTxt)
				{
					this.changeOutputPatternBtn.PerformClick();
				}
			}
		}

		#endregion

		#region Pattern Builder Tab controls and event handlers

		#region Manual

		private void newPatternBtn_Click(object sender, System.EventArgs e)
		{
			if(nn.learnAlgoProp.inputProp == null || nn.learnAlgoProp.teachOutputProp == null)
			{
				nn.learnAlgoProp.inputProp = new float[0][];
				nn.learnAlgoProp.teachOutputProp = new float[0][];
			}
			if(nn.learnAlgoProp.inputProp.Length < 1 || nn.learnAlgoProp.teachOutputProp.Length < 1)
			{
				nn.learnAlgoProp.inputProp = new float[1][];
				nn.learnAlgoProp.inputProp[0] = new float[nn.inputProp.Length];
				nn.learnAlgoProp.teachOutputProp = new float[1][];
				nn.learnAlgoProp.teachOutputProp[0] = new float[nn.outputLayerProp.Length];
			}
			else
			{
				float[][] inp = new float[nn.learnAlgoProp.inputProp.Length+1][];
				nn.learnAlgoProp.inputProp.CopyTo(inp,0);
				inp[inp.Length-1] = new float[nn.inputProp.Length];
				float[][] outp = new float[nn.learnAlgoProp.teachOutputProp.Length+1][];
				nn.learnAlgoProp.teachOutputProp.CopyTo(outp,0);
				outp[outp.Length-1] = new float[nn.outputLayerProp.Length];
				nn.learnAlgoProp.inputProp = inp;
				nn.learnAlgoProp.teachOutputProp = outp;
			}
			isDirty = true;
			this.updatePatternTreeView();
		}

		private void changeInputPatternBtn_Click(object sender, System.EventArgs e)
		{
			float num = 0.0f;
			try
			{
				if(nn == null)
					throw new Exception("No Neural Network generated!");
				if(this.inputTxt.Text.Length < 1)
					throw new Exception("Select a Neuron and type in the new Value");
				if(this.patternInputListView.SelectedItems.Count < 1)
					throw new Exception("No Neuron selected");
				num = float.Parse(this.inputTxt.Text);
			}
			catch(Exception ex)
			{
				MessageBox.Show(ex.Message, "Error");
				return;
			}
			this.patternInputListView.SelectedItems[0].SubItems[1].Text = this.inputTxt.Text;
			this.patternTreeView.SelectedNode.Nodes[0].Nodes[patternInputListView.SelectedItems[0].Index].Text = this.inputTxt.Text;
			nn.learnAlgoProp.inputProp[this.patternTreeView.SelectedNode.Index][patternInputListView.SelectedItems[0].Index] = num;
			this.inputTxt.Text = "";
			isDirty = true;
		}

		private void changeOutputPatternBtn_Click(object sender, System.EventArgs e)
		{
			float num = 0.0f;
			try
			{
				if(nn == null)
					throw new Exception("No Neural Network generated!");
				if(this.outputTxt.Text.Length < 1)
					throw new Exception("Select a Neuron and type in the new Value");
				if(this.patternOutputListView.SelectedItems.Count < 1)
					throw new Exception("No Neuron selected");
				num = float.Parse(this.outputTxt.Text);
			}
			catch(Exception ex)
			{
				MessageBox.Show(ex.Message, "Error");
				return;
			}
			this.patternOutputListView.SelectedItems[0].SubItems[1].Text = this.outputTxt.Text;
			this.patternTreeView.SelectedNode.Nodes[1].Nodes[patternOutputListView.SelectedItems[0].Index].Text = this.outputTxt.Text;
			nn.learnAlgoProp.teachOutputProp[this.patternTreeView.SelectedNode.Index][patternOutputListView.SelectedItems[0].Index] = num;
			this.outputTxt.Text = "";
			isDirty = true;
		}

		private void clearPaternsBtn_Click(object sender, System.EventArgs e)
		{
			clearManualPatternMode();
		}

		private void clearManualPatternMode()
		{
			this.patternInputListView.Items.Clear();
			this.patternOutputListView.Items.Clear();
			this.patternTreeView.SelectedNode = null;
			this.patternTreeView.Nodes.Clear();
			this.inputTxt.Text = "";
			this.outputTxt.Text = "";
		}

		private void currentPatternBtn_Click(object sender, System.EventArgs e)
		{
			try
			{
				if(nn == null)
					throw new NullReferenceException("No Neural Network generated!");
				else if(nn.learnAlgoProp.inputProp == null || nn.learnAlgoProp.teachOutputProp == null)
					throw new NullReferenceException("No Patterns yet.");
			}
			catch(NullReferenceException ex)
			{
				MessageBox.Show(ex.Message, "Error while reading current patterns");
				this.clearManualPatternMode();
				return;
			}

            updatePatternTreeView();
		}

		private void updatePatternTreeView()
		{
			this.clearManualPatternMode();
			if(nn.learnAlgoProp.inputProp != null && nn.learnAlgoProp.teachOutputProp != null)
			{
				TreeNode dad;
				for(int i=0; i<nn.learnAlgoProp.inputProp.Length; i++)
				{
					dad = new TreeNode("Pattern " + (i+1).ToString());
					TreeNode child = new TreeNode("Input");
					for(int j=0; j<nn.learnAlgoProp.inputProp[i].Length; j++)
						child.Nodes.Add(nn.learnAlgoProp.inputProp[i][j].ToString());
					dad.Nodes.Add(child);
					child = new TreeNode("Output");
					for(int j=0; j<nn.learnAlgoProp.teachOutputProp[i].Length; j++)
						child.Nodes.Add(nn.learnAlgoProp.teachOutputProp[i][j].ToString());
					dad.Nodes.Add(child);
					patternTreeView.Nodes.Add(dad);
				}
				if(nn.outputLayerProp.Length > 26)
				{
					this.assoziateNwLCB.Checked = false;
					this.assoziateNwLCB.Enabled = false;
				}
				else
					this.assoziateNwLCB.Enabled = true;
			}
		}

		private void patternTreeView_AfterSelect(object sender, System.Windows.Forms.TreeViewEventArgs e)
		{
			int selected;
			TreeNode tmp = this.patternTreeView.SelectedNode;
			ArrayList top10 = new ArrayList();

			do
			{
				selected = tmp.Index;
				tmp = tmp.Parent;
			} while(tmp != null);

			patternTreeView.SelectedNode = patternTreeView.Nodes[selected];
			this.inputTxt.Text = "";
			this.outputTxt.Text = "";

			this.patternInputListView.Items.Clear();
			this.patternOutputListView.Items.Clear();
			
			for(int i=0; i<this.nn.learnAlgoProp.inputProp[selected].Length; i++)
			{
				this.patternInputListView.Items.Add(new ListViewItem(new string[] { (i+1).ToString(),this.nn.learnAlgoProp.inputProp[selected][i].ToString() } ));
			}

			float[] output = null;

			nn.inputProp = nn.learnAlgoProp.inputProp[selected];
			output = nn.outputProp;

			int letterNum = 0;
			float[] teachOutput = this.nn.learnAlgoProp.teachOutputProp[selected];
			for(int i=0; i<this.nn.learnAlgoProp.teachOutputProp[selected].Length; i++)
			{
				this.patternOutputListView.Items.Add(new ListViewItem(new string[] {
						(i+1).ToString(),teachOutput[i].ToString(),
						output[i].ToString("0.0000000") } ));
				if(teachOutput[i] == 1f)
					letterNum = i;
				top10.Add(new outputWithNum(i,output[i]));
			}

			String str = "Top responding Neurons";

			if(this.assoziateNwLCB.Checked)
			{
				char c = (char)(this.letterRangeMinUpDown.Text[0]+letterNum);
				if(c >= 'A' && c <= 'Z')
					str += " for " + c;
			}

			this.top10_GroupBox.Text = str;
			// t-bä 10.10.
			if(top10.Count > 10)
				top10.Sort();

			float min = nn.actFuncForEachNeuronProp[0][0].minProp;
			float max = nn.actFuncForEachNeuronProp[0][0].maxProp;

			int j = 0;
			for(j=0; j<10 && j<top10.Count; j++)
			{
				float neuronOutput = ((outputWithNum)top10[j]).output;
				int neuronNum = ((outputWithNum)top10[j]).num;

//				progBars[j].Minimum = (int)Math.Floor((double)min);
//				progBars[j].Maximum = (int)Math.Ceiling((double)max);

				progBars[j].Value = (int)(((progBars[j].Maximum-progBars[j].Minimum)/(max-min))*
					(neuronOutput-min)+progBars[j].Minimum);
//				progBars[j].Value = (int)Math.Round((100*neuronOutput), 0);
				progBars[j].Visible = true;
				labels[j].Visible = true;

				labels[j].Text = "Neuron " + (neuronNum+1).ToString();
				if(this.assoziateNwLCB.Checked)
				{
					char c = (char)(this.letterRangeMinUpDown.Text[0]+neuronNum);
					if(c >= 'A' && c <= 'Z')
						labels[j].Text += " (" + c + ")";
				}
			}

			for(j=j; j<progBars.Length; j++)
			{
				progBars[j].Visible = false;
				labels[j].Visible = false;
			}
		}

		private void patternInputListView_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			if(this.patternInputListView.SelectedItems.Count != 0)
				this.inputTxt.Text = patternInputListView.SelectedItems[0].SubItems[1].Text;
		}

		private void patternOutputListView_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			if(this.patternOutputListView.SelectedItems.Count != 0)
				this.outputTxt.Text = patternOutputListView.SelectedItems[0].SubItems[1].Text;
		}

		private void deletePatternBtn_Click(object sender, System.EventArgs e)
		{
			try
			{
				if(nn.learnAlgoProp.inputProp.Length < 1 || nn.learnAlgoProp.teachOutputProp.Length < 1)
					throw new Exception("No Patterns yet!");
				if(patternTreeView.SelectedNode == null)
					throw new Exception("No Pattern selected!");
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "Error while deleting!");
				return;
			}
			float [][] inp = new float[nn.learnAlgoProp.inputProp.Length-1][];
			float [][] outp = new float[nn.learnAlgoProp.teachOutputProp.Length-1][];
			for(int i=0, j=0; i<nn.learnAlgoProp.inputProp.Length; i++)
			{
				// curent Pattern != selected Pattern
				if(i != this.patternTreeView.SelectedNode.Index)
				{
					// copy input
					inp[j] = nn.learnAlgoProp.inputProp[i];
					// copy output
					outp[j] = nn.learnAlgoProp.teachOutputProp[i];
					// increase index
					j++;
				}
			}

			nn.learnAlgoProp.inputProp = inp;
			nn.learnAlgoProp.teachOutputProp = outp;
			isDirty = true;
			this.updatePatternTreeView();
		}

		private void deleteAllPaternsBtn_Click(object sender, System.EventArgs e)
		{
			if(MessageBox.Show("Do you REALLY want to delete ALL Paterns?","Delete All?",MessageBoxButtons.YesNo) == DialogResult.Yes)
			{
				nn.learnAlgoProp.inputProp = null;
				nn.learnAlgoProp.teachOutputProp = null;
				clearPaternsBtn.PerformClick();
				isDirty = true;
			}
		}

		private void patternTreeExpand_Btn_Click(object sender, System.EventArgs e)
		{
			patternTreeView.ExpandAll();
		}

		private void patternTreeCollapse_Btn_Click(object sender, System.EventArgs e)
		{
			patternTreeView.CollapseAll();
		}

/*		private void manualModePanel_Paint(object sender, System.Windows.Forms.PaintEventArgs e)
		{
			if(this.patternTreeView.Nodes.Count != nn.learnAlgoProp.inputProp.Length)
			{
				this.currentPatternBtn.PerformClick();
			}
		}
*/
		// t-bä 09.10.
		class outputWithNum : IComparable
		{
			public outputWithNum(int number, float output)
			{
				this.num = number;
				this.output = output;
			}

			public readonly float output;
			public readonly int num;

			public int CompareTo(Object x)
			{
				return ((outputWithNum)x).output.CompareTo(this.output);
			}
		}

		#endregion

		# region OCR

		private void startFilterBtn_Click(object sender, System.EventArgs e)
		{	
			if(filterRadioBtn.Checked)
			{
				if(srcBmp == null)
				{
					MessageBox.Show("You have to load a picture first!\nNo filtering without a picture! ;)");
					return;
				}
				restartThread(new ThreadStart(calcImageFilters));
			}
			else if(creationRadioBtn.Checked)
				restartThread(new ThreadStart(calcOCRImages));
		}

		private void stopFilterBtn_Click(object sender, System.EventArgs e)
		{
			stopThread();
		}

		// changed
		// sets the state of the filter buttons
		private void setFilterBtnState(ThreadStates state)
		{
			if(state == ThreadStates.START)
			{
				startGeneratePatternsBtn.Enabled = false;
				stopGeneratePatternsBtn.Enabled = true;
			}
			else if(state == ThreadStates.STOP)
			{
				startGeneratePatternsBtn.Enabled = true;
				stopGeneratePatternsBtn.Enabled = false;
			}
		}

		private void charsBmpUpDown_SelectedItemChanged(object sender, System.EventArgs e)
		{			
			if(charBmpsAL == null)
				return;

			Graphics gfx = Graphics.FromImage(charPB.Image);
			gfx.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
			gfx.Clear(Color.White);
			gfx.DrawImage((Image)charBmpsAL[(sender as DomainUpDown).SelectedIndex], 0, 0, charPB.Width, charPB.Height);
			charPB.Refresh();
		}

		private void featureExtraktioncomboBox_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			this.counterGroupBox.Visible = false;
			this.segmGroupBox.Visible = false;
			Assembly asm = Assembly.LoadWithPartialName(OCR_PREPROC_ASMNAME);
			Type t = asm.GetType(extractFuncs[(sender as ComboBox).SelectedIndex, 1], true, true);
			if(t.BaseType == typeof(OCRImgExtCounter))
				this.counterGroupBox.Visible = true; 
			else if(t == typeof(ImgSegmenter))
				this.segmGroupBox.Visible = true; 
		}
		private void creationRadioBtn_CheckedChanged(object sender, System.EventArgs e)
		{
			ocrModePanel.Refresh();
		}

		private void ocrModePanel_Paint(object sender, System.Windows.Forms.PaintEventArgs pea)
		{
			int offY;
			if(creationRadioBtn.Checked)
			{
				offY = creationRadioBtn.Location.Y + (creationRadioBtn.Height>>1);
				startGeneratePatternsBtn.Top = offY - creationRadioBtn.Height - startGeneratePatternsBtn.Height;
				stopGeneratePatternsBtn.Top = offY + creationRadioBtn.Height;
				generateNetcheckBox.Enabled = true;
			}
			else
			{
				offY = filterRadioBtn.Location.Y + (filterRadioBtn.Height>>1);
				startGeneratePatternsBtn.Top = offY - filterRadioBtn.Height - startGeneratePatternsBtn.Height;
				stopGeneratePatternsBtn.Top = offY + filterRadioBtn.Height;
				generateNetcheckBox.Enabled = false;
				generateNetcheckBox.Checked = false;
			}

			float y = filterRadioBtn.Location.Y+6;
			if(creationRadioBtn.Checked)
				y += creationRadioBtn.Location.Y - filterRadioBtn.Location.Y;
			pea.Graphics.DrawLine(arrowPen, creationRadioBtn.Location.X+creationRadioBtn.Width, y, 
											this.sepImgGroupBox.Location.X, y);
	
			y = this.sepImgGroupBox.Location.Y+this.sepImgGroupBox.Height>>1;
			pea.Graphics.DrawLine(arrowPen, this.sepImgGroupBox.Location.X+this.sepImgGroupBox.Width, y, 
											this.featureExtractGroupBox.Location.X+this.featureExtractGroupBox.Width/2, y);
			pea.Graphics.DrawLine(arrowPen, this.featureExtractGroupBox.Location.X+this.featureExtractGroupBox.Width/2, y, 
											this.featureExtractGroupBox.Location.X+this.featureExtractGroupBox.Width/2,
											this.featureExtractGroupBox.Location.Y+this.featureExtractGroupBox.Height);
			startExtractBtn.Top = (int)(y - 3 - startExtractBtn.Height);
			stopExtractBtn.Top = (int)(y + arrowPen.Width);
		}
		
		private void extractWin_ValueChanged(object sender, System.EventArgs e)
		{
			if(xnUpDown.Value >= (decimal)(charPB.Width/charPBSx))
				xnUpDown.Value = (decimal)(charPB.Width/charPBSx);
			if(ynUpDown.Value >= (decimal)(charPB.Height/charPBSy))
				ynUpDown.Value = (decimal)(charPB.Height/charPBSy);
			if(widthnUpDown.Value <= 0 || heightnUpDown.Value <= 0)
				return;
			if(widthnUpDown.Value + xnUpDown.Value > (decimal)(charPB.Width/charPBSx))
				widthnUpDown.Value = (decimal)(charPB.Width/charPBSx) - xnUpDown.Value;
			if(heightnUpDown.Value + ynUpDown.Value > (decimal)(charPB.Height/charPBSy))
				heightnUpDown.Value = (decimal)(charPB.Height/charPBSy) - ynUpDown.Value;
			charPB.Refresh();
		}

		private void charPB_Paint(object sender, System.Windows.Forms.PaintEventArgs pea)
		{
			pea.Graphics.DrawRectangle(pen, (float)xnUpDown.Value*charPBSx, (float)ynUpDown.Value*charPBSy, 
											(float)widthnUpDown.Value*charPBSx-3, (float)heightnUpDown.Value*charPBSy-3);
		}
		private void selectFontBtn_Click(object sender, System.EventArgs e)
		{
			fontDialog.ShowDialog();
			if(creationRadioBtn.Checked)
				setCtrlsToBmp((new OCRImageCreator('W', fontDialog.Font)).bmpProp, true);
		}

		private void srcImgPB_Paint(object sender, System.Windows.Forms.PaintEventArgs pea)
		{
			pea.Graphics.DrawRectangle(pen, (float)xCharRgnnUpDown.Value*charRgnSx, (float)yCharRgnnUpDown.Value*charRgnSy,
											(float)widthCharRgnnUpDown.Value*charRgnSx-3, (float)heightCharRgnnUpDown.Value*charRgnSy-3);
		}

		private void charRgn_ValueChanged(object sender, System.EventArgs e)
		{
			if(xCharRgnnUpDown.Value >= (decimal)(srcImgPB.Width/charRgnSx))
				xCharRgnnUpDown.Value = (decimal)(srcImgPB.Width/charRgnSx);
			if(yCharRgnnUpDown.Value >= (decimal)(srcImgPB.Height/charRgnSy))
				yCharRgnnUpDown.Value = (decimal)(srcImgPB.Height/charRgnSy);
			if(widthCharRgnnUpDown.Value <= 0 || heightCharRgnnUpDown.Value <= 0)
				return;
			if(widthCharRgnnUpDown.Value + xCharRgnnUpDown.Value > (decimal)(srcImgPB.Width/charRgnSx))
				widthCharRgnnUpDown.Value = (decimal)(srcImgPB.Width/charRgnSx) - xCharRgnnUpDown.Value;
			if(heightCharRgnnUpDown.Value + yCharRgnnUpDown.Value > (decimal)(srcImgPB.Height/charRgnSy))
				heightCharRgnnUpDown.Value = (decimal)(srcImgPB.Height/charRgnSy) - yCharRgnnUpDown.Value;
			srcImgPB.Refresh();
		}

		private void startExtractBtn_Click(object sender, System.EventArgs e)
		{
			restartThread(new ThreadStart(extractImages)); 
		}

		// changed
		private void setExtractButtonState(ThreadStates state)
		{
			if(state == ThreadStates.START)
			{
				startExtractBtn.Enabled = false;
				stopExtractBtn.Enabled = true;
			}
			else if(state == ThreadStates.STOP)
			{
				startExtractBtn.Enabled = true;
				stopExtractBtn.Enabled = false;
			}
		}

		private void delImgBtn_Click(object sender, System.EventArgs e)
		{
			if(charsBmpUpDown.Enabled)
			{
				int index = this.charsBmpUpDown.SelectedIndex;

				charsBmpUpDown.Items.RemoveAt(index);
				charBmpsAL.RemoveAt(index);
				charsCount--;

				if(index >= this.charsBmpUpDown.Items.Count-1)
					this.charsBmpUpDown.SelectedIndex = this.charsBmpUpDown.Items.Count-1;
				else
					this.charsBmpUpDown.SelectedIndex = index;

				if(charsBmpUpDown.Items.Count == 0)
				{
					startExtractBtn.Enabled = false;
					stopExtractBtn.Enabled = false;
					delImgBtn.Enabled = false;
				}
			}
		}

		private void generateNetcheckBox_CheckedChanged(object sender, System.EventArgs e)
		{
			if(generateNetcheckBox.Checked)
			{
				addPatsCheckBox.Checked = false;
				addPatsCheckBox.Enabled = false;
			}
			else
				addPatsCheckBox.Enabled = true;
		}
		
		#endregion

		private void patternModeRadioBtn_CheckedChanged(object sender, System.EventArgs e)
		{
			if(ocrModeRadioBtn.Checked)
			{
				setCharRgnScaleCtrls(srcBmp, false);
				this.manualModePanel.Visible = false;
				this.ocrModePanel.Visible = true;
			}
			else
			{
				this.updatePatternTreeView();
				this.ocrModePanel.Visible = false;
				this.manualModePanel.Visible = true;
			}
		}

		private void assoziateNwLCB_CheckedChanged(object sender, System.EventArgs e)
		{
			if(patternTreeView.Nodes != null)
			{
				if(this.patternTreeView.SelectedNode != null)
				{
					this.patternTreeView_AfterSelect(patternTreeView, null);
				}
				else
				{
					patternTreeView.SelectedNode = patternTreeView.Nodes[0];
				}
			}
		}


		#endregion

		#region Net Trainer Tab controls event handlers

		private void startTrainBtn_Click(object sender, System.EventArgs e)
		{
			try	
			{ 
				if(nn.learnAlgoProp.inputProp == null || nn.learnAlgoProp.teachOutputProp == null)
					throw new Exception("No Patterns yet.");	
			}
			catch(Exception ex) 
			{
				MessageBox.Show(ex.Message);
				return;
			}
			restartThread(new ThreadStart(calcNetLearning));
		}
		private void stopTrainBtn_Click(object sender, System.EventArgs e)
		{
			stopThread();
			errorListView.EndUpdate();
			if(fastModeCheckBox.Checked)
				errorPB.Invalidate();
			this.learnAlgoComboBox.Enabled = true;
			this.infoSBP.Text = "Stopped after " + nn.learnAlgoProp.iterationProp + " cycles!";
		}

		private void learnAlgoComboBox_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			removeLearnAlgoParamCtrls();
			int mi = nn.learnAlgoProp.maxIterationProp;
			float maxErr = nn.learnAlgoProp.minTolerableErrorProp;
			float[][] te = nn.learnAlgoProp.teachOutputProp;
			float[][] inp = nn.learnAlgoProp.inputProp;
			int selected = ((ComboBox)sender).SelectedIndex;
			
			// t-bä was here
			nn.learnAlgoProp = this.learningAlgos[selected];
			
			switch(selected)
					//			switch((sender as ComboBox).SelectedIndex)
			{
				case 0:
					//					nn.learnAlgoProp = backPropLearnAlgo;
					isBackpropBatchMethod = false;
					break;
				case 1:
					//					nn.learnAlgoProp = backPropLearnAlgo;
					isBackpropBatchMethod = true;
					break;
				case 2:
					//					nn.learnAlgoProp = geneticLearnAlgo;
					isBackpropBatchMethod = false;
					break;
				default:
					goto case 0;
			}
			nn.learnAlgoProp.maxIterationProp = mi;
			nn.learnAlgoProp.minTolerableErrorProp = maxErr;
			nn.learnAlgoProp.teachOutputProp = te;
			nn.learnAlgoProp.inputProp = inp;
			nn.learnAlgoProp.nnProp = nn;
			createLearnAlgoParamCtrls();
			isDirty = true;
		}

		private void removeLearnAlgoParamCtrls()
		{
			if(learnAlgoParamAdjuster != null)
				for(int i=0; i<nn.learnAlgoProp.algoProps.Length; i++)
					this.trainPage.Controls.Remove(learnAlgoParamAdjuster[i]);
		}

		private void createLearnAlgoParamCtrls()
		{	
			//r-s-21.9.
			if(nn.actFuncForEachNeuronProp[0][0].GetType().BaseType == typeof(SigmoidActFunc))
				steepnessAdjuster.Visible = true;
			else
				steepnessAdjuster.Visible = false;

			cyclesUpDown.Value = nn.learnAlgoProp.maxIterationProp;
			maxErrorUpDown.Value = (decimal)nn.learnAlgoProp.minTolerableErrorProp;
			this.learnAlgoParamAdjuster = new ParamAdjuster.ParamAdjuster[nn.learnAlgoProp.algoProps.Length];
			Type laType = nn.learnAlgoProp.GetType();
			for(int i=0; i<nn.learnAlgoProp.algoProps.Length; i++)
			{
				float val = (float)laType.GetProperty((nn.learnAlgoProp.algoProps[i] + "Prop")).GetValue(nn.learnAlgoProp, null);
				float valMin = (float)laType.GetProperty((nn.learnAlgoProp.algoProps[i] + "MinProp")).GetValue(nn.learnAlgoProp, null);
				float valMax = (float)laType.GetProperty((nn.learnAlgoProp.algoProps[i] + "MaxProp")).GetValue(nn.learnAlgoProp, null);
				float valStep = (float)laType.GetProperty((nn.learnAlgoProp.algoProps[i] + "StepProp")).GetValue(nn.learnAlgoProp, null);
				
				this.learnAlgoParamAdjuster[i] = new ParamAdjuster.ParamAdjuster(val, valMin, valMax, valStep, nn.learnAlgoProp.algoPropsDescription[i]);
				this.learnAlgoParamAdjuster[i].Anchor = ((AnchorStyles)(((AnchorStyles.Bottom | AnchorStyles.Left) | AnchorStyles.Right)));
				this.learnAlgoParamAdjuster[i].Location = new Point(steepnessAdjuster.Location.X,
					steepnessAdjuster.Location.Y + (i+1)*learnAlgoParamAdjuster[i].Size.Height);
				this.learnAlgoParamAdjuster[i].Name = "paramAdjuster" + i;
				this.learnAlgoParamAdjuster[i].Size = steepnessAdjuster.Size;
				this.learnAlgoParamAdjuster[i].TabIndex = steepnessAdjuster.TabIndex + 1 + i;
				this.learnAlgoParamAdjuster[i].onNewValue += new ParamAdjuster.valueHandler(paramAdjuster_onNewValue);
				this.paramAdjuster_onNewValue(this.learnAlgoParamAdjuster[i], val);
				this.trainPage.Controls.Add(this.learnAlgoParamAdjuster[i]);
			}
		}

		private void paramAdjuster_onNewValue(Object sender, float newValue)
		{
			for(int i=0; i<nn.learnAlgoProp.algoProps.Length; i++)
			{
				if(nn.learnAlgoProp.algoPropsDescription[i] == (sender as ParamAdjuster.ParamAdjuster).paramNameProp)
					nn.learnAlgoProp.GetType().GetProperty((nn.learnAlgoProp.algoProps[i] + "Prop")).SetValue(nn.learnAlgoProp, newValue, null);
			}
			isDirty = true;
		} 

		private void steepnessAdjuster_onNewValue(object sender, float newValue)
		{
			//r-s-21.9.
			if(nn.actFuncForEachNeuronProp[0][0].GetType().BaseType == typeof(SigmoidActFunc))
				steepnessOfAllSigmoidNeuronsProp = newValue;
			isDirty = true;
		}

		private void cyclesUpDown_ValueChanged(object sender, System.EventArgs e)
		{		
			nn.learnAlgoProp.maxIterationProp = (int)(sender as NumericUpDown).Value;	
			updateAxisText();
			calcScaleFactors();
			this.errorPB.Invalidate();
			isDirty = true;
		}
		private void maxErrorUpDown_ValueChanged(object sender, System.EventArgs e)
		{
			nn.learnAlgoProp.minTolerableErrorProp = (float)(sender as NumericUpDown).Value;
			isDirty = true;
		}

		#endregion

		#region Menu event handlers

		private void fileMenuNewItem_Click(object sender, System.EventArgs e)
		{
			if(checkDirtyState() == DialogResult.Cancel && nn != null)
				return;
			try
			{
				try
				{
					loadNet(DEF_NET_PATH);
				}
				catch(System.IO.DirectoryNotFoundException exc)
				{
					throw new System.IO.FileNotFoundException(exc.Message, exc);
				}
				catch(ArgumentException exc)
				{
					throw new System.IO.FileNotFoundException(exc.Message, exc);
				}
			}
			catch(System.IO.FileNotFoundException exc)
			{
				Console.WriteLine("This exception: " + exc.StackTrace);
				if(exc.InnerException != null)
					Console.WriteLine("Inner exception: " + exc.InnerException.StackTrace);
				MessageBox.Show("Default Network was not found in the examples/net folder.\n" + 
					"Please choose the location of the file \"default.net\" or load another .net file",
					"Error loading default.net");
				openFileDialogNN.FileName = "default.net";
				if(openFileDialogNN.ShowDialog() == DialogResult.OK)
					loadNet(openFileDialogNN.FileName);
				else
					throw exc;
			}
			associateLearnAlgosWithNet(nn);
			nn.randomizeWeights();
			extractFilenameAndSetTitle("", false);
		}

		private void fileMenuLoadItem_Click(object sender, System.EventArgs e)
		{
			if(checkDirtyState() == DialogResult.Cancel)
				return;
			if(openFileDialogNN.ShowDialog() == DialogResult.OK)
			{
				loadNetAndSetCtrls(openFileDialogNN.FileName);
			}
		}

		private void fileMenuSaveItem_Click(object sender, System.EventArgs e)
		{
			// t-bä
			saveFileDialogNN.FileName = nn.netNameProp;
			//r-s
			//	saveFileDialogNN.FileName = openFileDialogNN.FileName;
			if(saveFileDialogNN.ShowDialog() == DialogResult.OK)
			{
				FileManager.getInstance().writeNetworkToXml(saveFileDialogNN.FileName, nn);
				extractFilenameAndSetTitle(saveFileDialogNN.FileName, true);
				//r-s
				openFileDialogNN.FileName = saveFileDialogNN.FileName;
				isDirty = false;
			}
		}

		private void fileMenuLoadPicItem_Click(object sender, System.EventArgs e)
		{
			if(openFileDialogPic.ShowDialog() == DialogResult.OK)
			{
				srcBmp = new Bitmap(openFileDialogPic.FileName);
				setCharRgnScaleCtrls(srcBmp, true);
				filteredImgPB.Image = null;
			}
		}

		private void fileMenuLoadPattern_Click(object sender, System.EventArgs e)
		{
			if(checkDirtyState() == DialogResult.Cancel)
				return;

			if(DialogResult.OK == this.openFileDialogPattern.ShowDialog())
			{
				Patterns pat = FileManager.getInstance().readPatternsFromXml(openFileDialogPattern.FileName);
				try
				{
					if(pat.inputsProp[0].Length != nn.learnAlgoProp.inputProp[0].Length)
						throw new Exception("Length of Input from Pattern 1 is not compatible to the current Neural Net!");
					if(pat.teachOutputsProp[0].Length != nn.learnAlgoProp.teachOutputProp[0].Length)
						throw new Exception("Length of TeachOutput from Pattern 1 is not compatible to the current Neural Net!");
				}
				catch(Exception ex)
				{
					MessageBox.Show(ex.Message, "Error while loading patterns");
					return;
				}
				nn.learnAlgoProp.inputProp = pat.inputsProp;
				nn.learnAlgoProp.teachOutputProp = pat.teachOutputsProp;
				isDirty = true;
			}
		}

		private void fileMenuSavePattern_Click(object sender, System.EventArgs e)
		{
			if(DialogResult.OK == this.saveFileDialogPattern.ShowDialog())
			{
				FileManager.getInstance().writePatternsToXml(saveFileDialogPattern.FileName,
					nn.learnAlgoProp.inputProp, nn.learnAlgoProp.teachOutputProp);
			}
		}

		private void fileMenuExitItem_Click(object sender, System.EventArgs e)
		{
			if(checkDirtyState() == DialogResult.Cancel)
				return;
			Application.Exit();
		}

		private void netMenuRandomizeItem_Click(object sender, System.EventArgs e)
		{	
			// changed, kommentare eingefügt, verwechslung beseitigt
			if(nn != null)
			{
				Console.WriteLine("time: " + useTimeAsSeed + "\nrange: " + useBestRange);
				// time as seed checked
				if(useTimeAsSeed & !useBestRange)
					nn.randomizeWeights(this.rangeMin, this.rangeMax);
				// best range checked
				else if(!useTimeAsSeed & useBestRange)
					nn.randomizeWeights(this.randSeed);
				// both checked
				else if(useTimeAsSeed & useBestRange)
					nn.randomizeWeights();
				// nothing checked
				else if(!useTimeAsSeed & !useBestRange)
					nn.randomizeWeights(this.rangeMin, this.rangeMax, this.randSeed);
				
				isDirty = true;
			}
		}
		private void netMenuStartStopThreadItem_Click(object sender, System.EventArgs e)
		{
			if(calcThread != null)
			{
				ThreadState state = calcThread.ThreadState;
				if(state == ThreadState.Running)
					this.stopTrainBtn.PerformClick();
				else
					this.startTrainBtn.PerformClick();
			}
			else
				this.restartThread(new ThreadStart(this.calcNetLearning));
		}

		private void infoMenuAboutItem_Click(object sender, System.EventArgs e)
		{
			AboutForm af = new AboutForm();
			af.ShowDialog();
			af.Dispose();
		}

		private void infoMenuHelpItem_Click(object sender, System.EventArgs e)
		{
			System.Diagnostics.Process.Start(Application.StartupPath + @"\doc\manual.htm");
		}
		#endregion

		#endregion
	}
}