/*
 * Parse3PdfTextsElectionDataForWayneCountyMI.cs
 *
 * which code and results I will archive at:
 * https://github.com/IneffablePerformativity
 * https://github.com/IneffablePerformativity/Parse3PdfTextsElectionDataForWayneCountyMI
 * 
 * 
 * "To the extent possible under law, Ineffable Performativity has waived all copyright and related or neighboring rights to
 * The C# program Parse3PdfTextsElectionDataForWayneCountyMI.cs and resultant outputs.
 * This work is published from: United States."
 * 
 * This work is offered as per license: http://creativecommons.org/publicdomain/zero/1.0/
 * 
 * 
 * I wrote a PDF-exported-TXT parser for Wayne County Michigan 2020 election data,
 * bolted it to my CSV-output and plotter, which builds on these prior successes:
 * github.com/IneffablePerformativity/ParseClarityElectionDataForOaklandCountyMI
 * github.com/IneffablePerformativity/ParseClarityElectionDataForStateOfGeorgia
 * github.com/IneffablePerformativity/ParseClarityElectionDataForStateOfColorado
 *
 * 
 * But all 4 prior programs had an error, just fixed, soon to fix in those.
 * To Wit, I did not make a "Four" thing upon each grain, but outside loop.
 * Therefore, all the vote counts were merging into forever ascending sums.
 * Corrected on 2020-11-26. Henceforth you can trust me, 'til next mistake.
 * 
 * also note, I manually compress final image at tinypng.com
 */


using System;
using System.Collections.Generic;
using System.Drawing; // Must add a reference to System.Drawing.dll
using System.Drawing.Imaging; // for ImageFormat
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml;


namespace Parse3PdfTextsElectionDataForWayneCountyMI
{
	
	class Choice // AKA Candidate
	{
		public Choice(string party)
		{
			this.party = party;
		}
		public string party = "";
		public int votes = 0;
	}
	
	class Contest // AKA Race
	{
		public Dictionary<string,Choice> choices = new Dictionary<string,Choice>();
	}
	
	class Grain // AKA Precinct, County, Ward, etc.
	{
		public Grain(string locality)
		{
			this.locality = locality;
		}
		public string locality = "";
		public Dictionary<string,Contest> contests = new Dictionary<string,Contest>();
	}
	
	class ShrodingersCat // needed to make a reference to N sums, which one unknown yet
	{
		public int rep;
		public int dem;
		public int etc;
	}

	class Program
	{
		static string LocationBeingStudied = "WayneCountyMI";

		static string LocationBeingStudiedClearly = "Wayne County, Michigan";

		static string gitHubRepositoryURL = "https://github.com/IneffablePerformativity/Parse3PdfTextsElectionDataForWayneCountyMI";

		static string gitHubRepositoryShortened = "https://bit.ly/3m6KfX3";
		
		// Now generated on the fly as I mark the Xs:
		static string finalConclusion = "FWIW, The CISR Rumor Control page says undervote (here 'Bonus') is not a sign of fraud. YMMV.";

		static bool discardStraightPartyVotes = false; // affects bonus, csv, more than plot

		// Straight-Party votes add to all (POTUS,SENUS,REPUS)
		
		static bool useConceptOfStraightPartyVotes = false; // only affects messages

		// If this worked, it should make the 2 POTUS votes coincide. Yes, it does work.
		// Ah, but the divisor change affects Potus and Other: no net effect on Bonuses.

		static bool omitEtcPotusFromTotalBallots = false;
		
		static int howOther = 2; // 1=average of Sen+Rep, 2=Sen, 3=Rep

		// This should de-noise plot.
		// using 100 here eliminates about 90 CSV rows, leaves about 1000 rows in Wayne.Co.
		
		static int someMinimumTotalBallots = 500;
		
		// The LEFT of graph shows nothing interesting (sorted by Republicanism).
		// This should chop it off so i need not stretch image so much.
		static int someMinimumRepublicanism = 400000; // 40%
		
		// plot abscissa orderings are editted in code.
		// By this principle, unless changed in code:

		static string orderingPrinciple = "ascending Republicanism (red area)"; // edit code below to modify.

		// well, why not have a choice up here?
		static int howOrdering = 0; // 0 gives the default just stated, 1-7 change it.
		
		
		// Grains are smallest locality, perhaps a Ward, county, precinct...

		// Wayne County MI shows cities, counties, precincts and more!
		const string GrainTag = "city,etc."; // XML tag name e.g., Precinct, County, Ward, etc.

		
		
		// inputting phase

		
		static string inputDir = @"C:\A\SharpDevelop\Parse3PdfTextsElectionDataFor" + LocationBeingStudied;
		
		static string inputXmlFileName = LocationBeingStudied + "-detail.xml"; // sitting there

		// This is the top-level store of all grains, held by locality name
		
		static Dictionary<string,Grain> grains = new Dictionary<string,Grain>();

		
		// ====================
		// Analyzing Phase
		// ====================

		
		// Literal strings for a switch case

		// if applicable, adds REP or DEM count to all contests
		const string contestStraight = "Straight Party Ticket";

		// Sum of all party's votes for POTUS race in each grain
		// become my approximation of the total Ballots in grain.
		const string contestPotus = "President of the United States";

		// zero, one, or two
		const string contestSenus1 = "United States Senator (Vote for 1)"; // if applicable
		
		// I read that ALL VOTERS get to vote in one Congress contest.
		// I only see Potus & Repus in PA, so will use a default case.
		const string contestRepus1 = "1st Congressional District";
		const string contestRepus2 = "2nd Congressional District";
		//qty tbd...

		
		// outputting phase
		
		

		
		static string DateTimeStr = DateTime.Now.ToString("yyyyMMddTHHmmss");
		
		
		static string DateStampPlot = DateTime.Now.ToString("yyyy-MM-dd");

		// Log outputs exploratory, debug data:

		static string logFilePath = @"C:\A\" + DateTimeStr + "_Parse3PdfTextsElectionDataFor" + LocationBeingStudied + "_log.txt";
		static TextWriter twLog = null;

		// this sum is to plot widths of each grain
		// proportional to its population = ballots.
		
		static int SumOfGrainTotalBallots = 0;

		// a favorite output idiom

		static void say(string msg)
		{
			if(twLog != null)
				twLog.WriteLine(msg);
			else
				Console.WriteLine(msg);
		}

		// Csv outputs voting data to visualize in Excel

		static string csvFilePath = @"C:\A\" + DateTimeStr + "_Parse3PdfTextsElectionDataFor" + LocationBeingStudied + "_csv.csv";
		static TextWriter twCsv = null;

		static void csv(string msg)
		{
			if(twCsv != null)
				twCsv.WriteLine(msg);
		}

		// this cleans csv output text

		static Regex regexNonAlnum = new Regex(@"\W", RegexOptions.Compiled);
		
		static char [] caComma = { ',' };

		
		// Png outputs my own bitmap for fine grained control of data display:

		static string pngFilePath = @"C:\A\" + DateTimeStr + "_Parse3PdfTextsElectionDataFor" + LocationBeingStudied + "_png.png";

		
		// Affects pen width choices!

		// Stretch the plot X direction due to so many data:
		const int stretchFactor = 4; // need 4 to see any details
		
		const int imageSizeX = stretchFactor * 1920 * 2;
		const int imageSizeY = 1080 * 2;
		const int nBorder = 100 * 2; // keep mod 10 == 0, now also mod 20 == 0
		
		const int plotSizeX = imageSizeX - 2 * nBorder;
		const int plotSizeY = imageSizeY - 2 * nBorder + 1; // so plotSizeY mod 10 == 1 for 11 graticules
		

		// When I fixed the 'four' mistake, 2020-11-27, bonuses jumped up!
		// Old 11 graticules with full scale +-5% doesn't cut it any more.

		static bool do21gratsfspm10 = true;

		// on PPM, plot full scale of 1M = 100% (of total votes in grain)
		// if desire bonus plot full scale = +/-5% then scale up by 10.
		// if desire bonus plot full scale = +/-10% then scale up by 5.
		
		static int scaleFactor = (do21gratsfspm10 ? 5 : 10);
		
		// N.B. At current 21 graticules, 200K = 4%
		// but at 11 graticules, 200K = 2%
		// Shooting for 3% now:
		static int XMarksTheSpotThreshold = (do21gratsfspm10 ? 150000 : 30000);

		static int countTheXMarksTheSpot = 0;

		static int effectOnVotes = 0;
		
		public static void Main(string[] args)
		{
			Console.WriteLine("Hello World!");
			
			// TODO: Implement Functionality Here
			
			using(twLog = File.CreateText(logFilePath))
				using(twCsv = File.CreateText(csvFilePath))
			{
				try { doit(); } catch(Exception e) {say(e.ToString());}
			}

			Console.Write("Press any key to continue . . . ");
			Console.ReadKey(true);
		}

		
		static void doit()
		{
			// Phase one -- Inputting data
			
			if(useConceptOfStraightPartyVotes)
				say("discardStraightPartyVotes = " + discardStraightPartyVotes);
			
			// inputDetailXmlFile(); // unused in this app
			
			inputDataFrom3PdfToTextExportedFiles();

			// diagnosticDump(); // optional, not needed
			

			// Phase two -- Analyze data
			
			LetsRaceTowardTheDesideratum();

			
			// Phase three -- Output data
			
			// I scale [0.0% to 1.0%] into ppm = Parts Per Million [0 to 1000000].
			// all ppm are computed with divisor = totalBallots in grain locality.

			if(twCsv != null)
				csv("ordering,ppmRepPotus,ppmDemPotus,1M-ppmDemPotus,ppmRepOther,ppmDemOther,1M-ppmDemOther,BonusToTrump,BonusToBiden,totalBallots,locality");

			OutputTheCsvResults();
			
			OutputTheStatistics();

			OutputThePlotResults();

			// I just realized, but in too big a hurry to reverse it,
			// but the 3 OMITTING factors affect CSV as well as plot.
			say("Note that all items omitted above affect the CSV as well as the PNG.");
		}

		

		// New Front-End to existing application:
		
		// Just change to these new contest literals in
		// 1. switch(contest) and 2. switch(c) of old code:
		
		//"President/Vice-President (Vote for 1)";
		//"United States Senator (Vote for 1)";
		//"Representative in Congress 14th District (Vote for 1)";
		//"Representative in Congress 13th District (Vote for 1)";
		//"Representative in Congress 12th District (Wayne County pcts only) (Vote for 1)";
		//"Representative in Congress 11th District (Wayne County pcts only) (Vote for 1)";
		
		
		//Nobody knows the trouble I've seen.
		//Because I deleted development code.
		
		
		// Regulate all CRLFs to \n.
		// Also rid \f = form feeds.

		static Regex regexCRLFs = new Regex("\f|\r\n|\r|\n", RegexOptions.Compiled);

		
		// Rid the Page: ... lines
		
		static Regex regexPageNoTimestampLine = new Regex("^Page: .*$", RegexOptions.Compiled | RegexOptions.Multiline);

		
		// Rid the commas out of strings consisting of digits only
		
		static Regex regexCommasDigits = new Regex(@"\b[0-9,]+\b", RegexOptions.Compiled);
		//
		// which regex works with this evaluator function:
		//
		public static string replaceCommasDigitsEvaluator(Match match)
		{
			return match.Groups[0].Value.Replace(",", "");
		}

		
		// Here are the names of all the contests

		const string c1 = @"President/Vice-President \(Vote for 1\)";
		const string c2 = @"United States Senator \(Vote for 1\)";
		const string c3 = @"Representative in Congress 14th District \(Vote for 1\)";
		const string c4 = @"Representative in Congress 13th District \(Vote for 1\)";
		const string c5 = @"Representative in Congress 12th District \(Wayne County pcts only\) \(Vote for 1\)";
		const string c6 = @"Representative in Congress 11th District \(Wayne County pcts only\) \(Vote for 1\)";

		
		static Regex regexMatchContestNames = new Regex(
			"(?<contest>"+c1+"|"+c2+"|"+c3+"|"+c4+"|"+c5+"|"+c6+")",
			RegexOptions.Compiled);

		
		// I see lots of lines with candidate names.
		// Let me manually extract those explicitly.

		// President = POTUS -- puzzle parts:
		//145 Trump/Michael R.
		//145 Rocky De LaFuente/DarcyRichardson(NLP)
		//145 Pence(REP)
		//145 Joseph R.
		//145 JoJorgensen/JeremyCohen(LIB)
		//145 HowieHawkins/AngelaWalker(GRN)
		//145 Harris(DEM)
		//145 DonBlankenship/William Mohr(UST)
		//145 Donald J.
		//145 Biden/Kamala D.

		// Match newline now, or maybe space later
		
		const string snl = "[ \n]+";

		const string p1 = @"Joseph R."+snl+"Biden/Kamala D."+snl+@"Harris\(DEM\)";
		const string p2 = @"Donald J."+snl+"Trump/Michael R."+snl+@"Pence\(REP\)";
		const string p3 = @"JoJorgensen/JeremyCohen\(LIB\)";
		const string p4 = @"DonBlankenship/William Mohr\(UST\)";
		const string p5 = @"HowieHawkins/AngelaWalker\(GRN\)";
		const string p6 = @"Rocky De LaFuente/DarcyRichardson\(NLP\)";
		
		static Regex regexMatchPotusChoiceName = new Regex(
			"(?<choice>("+p1+"|"+p2+"|"+p3+"|"+p4+"|"+p5+"|"+p6+"))",
			RegexOptions.Compiled);

		
		//Senate = SENUS
		
		//145 Valerie L. Willis(UST)
		//145 Marcia Squier(GRN)
		//145 John James(REP)
		//145 Gary Peters(DEM)
		//145 Doug Dern(NLP)

		const string s1 = @"Valerie L. Willis\(UST\)";
		const string s2 = @"Marcia Squier\(GRN\)";
		const string s3 = @"John James\(REP\)";
		const string s4 = @"Gary Peters\(DEM\)";
		const string s5 = @"Doug Dern\(NLP\)";
		
		
		// Congress = REPUS - comes in four "sizes":
		
		//64 Sam Johnson(WCP)
		//64 Rashida Tlaib(DEM)
		//64 DavidDudenhoefer(REP)
		//64 D. Etta Wilcoxon(GRN)
		//64 Articia Bomer(UST)

		const string r1 = @"Sam Johnson\(WCP\)";
		const string r2 = @"Rashida Tlaib\(DEM\)";
		const string r3 = @"DavidDudenhoefer\(REP\)";
		const string r4 = @"D. Etta Wilcoxon\(GRN\)";
		const string r5 = @"Articia Bomer\(UST\)";

		//42 Robert VancePatrick(REP)
		//42 Philip Kolody(WCP)
		//42 Lisa Lane Gioia(LIB)
		//42 Clyde K. Shabazz(GRN)
		//42 Brenda Lawrence(DEM)

		const string r6 = @"Robert VancePatrick\(REP\)";
		const string r7 = @"Philip Kolody\(WCP\)";
		const string r8 = @"Lisa Lane Gioia\(LIB\)";
		const string r9 = @"Clyde K. Shabazz\(GRN\)";
		const string r10 = @"Brenda Lawrence\(DEM\)";

		//26 Jeff Jones(REP)
		//26 Gary Walkowicz(WCP)
		//26 Debbie Dingell(DEM)

		const string r11 = @"Jeff Jones\(REP\)";
		const string r12 = @"Gary Walkowicz\(WCP\)";
		const string r13 = @"Debbie Dingell\(DEM\)";

		//16 Leonard Schwartz(LIB)
		//16 Haley Stevens(DEM)
		//16 Eric S. Esshaki(REP)

		const string r14 = @"Leonard Schwartz\(LIB\)";
		const string r15 = @"Haley Stevens\(DEM\)";
		const string r16 = @"Eric S. Esshaki\(REP\)";


		
		// Three categories were nice, but lump ALL choices together:
		static Regex regexMatchEveryChoiceName = new Regex(

			"(?<choice>("+
			p1+"|"+p2+"|"+p3+"|"+p4+"|"+p5+"|"+p6+"|"+
			s1+"|"+s2+"|"+s3+"|"+s4+"|"+s5+"|"+
			r1+"|"+r2+"|"+r3+"|"+r4+"|"+r5+"|"+r6+"|"+
			r7+"|"+r8+"|"+r9+"|"+r10+"|"+r11+"|"+r12+"|"+
			r13+"|"+r14+"|"+r15+"|"+r16+"|Times Cast"+ // special, to match a table header
			"))",
			RegexOptions.Compiled);

		
		// Last extraneous text atop table is "Wayne County", twice.
		// also must rid this string: Total VotesUnresolvedWrite-In

		static Regex regexExtraneous = new Regex("^ *(Total VotesUnresolvedWrite-In *|Wayne County *)*",RegexOptions.Compiled);

		
		// Final way for doing one page table data block
		// with no extraneous text, and without newlines:

		static Regex regexTableRowSpliter = new Regex(
			"(?<spliter>Election Day|AV Counting Board|Total)",
			RegexOptions.Compiled);
		
		
		// to extract 3-char party name from candidate choice
		
		static Regex regexParty = new Regex(@"\((?<party>...)\)", RegexOptions.Compiled);

		
		// to rid spilled over numbers from grain/locality name
		
		static Regex regexSpilledNumbers = new Regex(@"^[0-9 ]+", RegexOptions.Compiled);


		// to match vote counts(s)
		
		static Regex regexVoteCounts = new Regex(@"([0-9]+)", RegexOptions.Compiled);

		
		static void inputDataFrom3PdfToTextExportedFiles()
		{
			string [] inputFilePaths = Directory.GetFiles(inputDir, "*.txt");

			foreach(string inputFilePath in inputFilePaths)
			{
				//Renamed to ensure POTUS first, then SENUS, then REPUS:
				//1-statementnov20prez.txt
				//2-statementussennov20.txt
				//3-tatementnov20cong.txt

				string body = File.ReadAllText(inputFilePath);
				
				// first, regulate, maybe later remove, CRLFs:
				body = regexCRLFs.Replace(body, "\n");
				
				// Second, rid the Page: ... timestamp lines, formfeeds
				body = regexPageNoTimestampLine.Replace(body, "");

				// Third, rid the commas out of strings of digits only
				body = regexCommasDigits.Replace(body, replaceCommasDigitsEvaluator);


				// contest delimiter, being in a capture group, will appear amongst parts
				
				string [] parts = regexMatchContestNames.Split(body);

				for(int i = 1; i < parts.Length - 1; i += 2)
				{
					doContestBlock(parts[i], parts[i+1]);
				}
			}
		}
		
		static void doContestBlock(string contest, string body)
		{
			contest = regexCRLFs.Replace(contest, "");
			
			// As henceforth need no newlines,
			// change them all to space first.
			
			body = regexCRLFs.Replace(body, " ");
			
			string [] parts = regexMatchEveryChoiceName.Split(body);

			string priorChoice = "";
			List<string>tableHeaders = new List<string>();
			foreach(string part in parts)
			{
				if(regexMatchEveryChoiceName.IsMatch(part))
				{
					if(part == priorChoice) // de-duplicate
						continue;
					priorChoice = part;
					
					tableHeaders.Add(part);
				}
				else if(part.Contains("Election Day"))
				{
					// process as per just-listed table headers
					int nHdr = tableHeaders.Count;
					
					if(nHdr == 1 && tableHeaders[0] == "Times Cast"
					   || nHdr == 2 && tableHeaders[1] == "Times Cast") // after Precinct
					{
						// "Times Cast" tables alternate with the tables of interest.

						// I need not process total ballots cast.
						
						// reset to start another table
						tableHeaders.Clear();

						continue;
					}

					// extract party names from choices
					string[]tableHeaderParties = new string[nHdr];
					for(int i = 0; i < nHdr; i++)
					{
						Match m = regexParty.Match(tableHeaders[i]);
						if(m.Success)
							tableHeaderParties[i] = m.Groups["party"].Value;
						else
							tableHeaderParties[i] = "";
					}

					{
						// I was passed the contest;
						// I am holding 1-n choices;
						// Foreach quaterion (locality and votes),
						// I still need to develop, for each votes:
						// string party, string grain, string votes

						string rest = regexExtraneous.Replace(part, "");
						
						// I now have a nice clean block of rows.
						
						string[]TableRowParts = regexTableRowSpliter.Split(rest);

						for(int i = 1; i < TableRowParts.Length - 2; i++)
						{
							if(TableRowParts[i] == "Election Day" &&
							   TableRowParts[i+2] == "AV Counting Board")
							{
								// clean the grain/locality name
								string grain = regexSpilledNumbers.Replace(TableRowParts[i-1], "").Trim();
								
								if(grain != "Total")
								{
									// sum votes, distribute to choices.
									// Getting close to the pot of gold.
									
									MatchCollection mc1 = regexVoteCounts.Matches(TableRowParts[i+1]);
									
									MatchCollection mc3 = regexVoteCounts.Matches(TableRowParts[i+3]);

									// produce the required call, for all choice columns
									for(int j = 0; j < nHdr; j++)
									{
										string choice = tableHeaders[j];
										string party = tableHeaderParties[j];
										string votes = (int.Parse(mc1[j].Value) + int.Parse(mc3[j].Value)).ToString();
										ponderInput(contest, choice, party, grain, votes);
									}
									
									i += 4;
								}
							}
						}
					}
					// reset to start another table
					tableHeaders.Clear();
				}
			}
		}

		
		// Unused in this special PDF version
		
		/*...................................................................
		 * 
		static void inputDetailXmlFile()
		{
			XmlDocument doc = new XmlDocument();
			doc.Load(Path.Combine(projectDirectoryPath,inputXmlFileName));

			XmlNode root = doc.DocumentElement;
			
			// XML descent
			
			XmlNodeList xnlContests = root.SelectNodes("Contest");
			foreach(XmlNode xnContest in xnlContests)
			{
				string contest = xnContest.Attributes["text"].Value;
				
				// Having just run through all to see the lay of it,
				// Only process contest of interest to my statistic.
				
				switch(contest)
				{
					case "Straight Party Ticket":
						if(discardStraightPartyVotes)
							continue;
						break; // Else, Process this category

						// sum of POTUS votes == total ballots in Oakland County:
					case "President/Vice-President (Vote for 1)":
						// sum of US Senator votes == total ballots in Oakland County:
					case "United States Senator (Vote for 1)":
						// sum of these 4 REP races == total ballots in Oakland County:
					case "Representative in Congress 14th District (Vote for 1)":
					case "Representative in Congress 13th District (Vote for 1)":
					case "Representative in Congress 12th District (Wayne County pcts only) (Vote for 1)":
					case "Representative in Congress 11th District (Wayne County pcts only) (Vote for 1)":
						break; // Process this category

					default:
						continue; // discard all others
				}

				XmlNodeList xnlChoices = xnContest.SelectNodes("Choice");
				foreach(XmlNode xnChoice in xnlChoices)
				{
					string choice = xnChoice.Attributes["text"].Value;
					string party = "";
					try { party = xnChoice.Attributes["party"].Value; } catch(Exception) { };


					// 1. Real votes
					XmlNodeList xnlGrains = xnChoice.SelectNodes("VoteType/" + GrainTag);
					foreach(XmlNode xnGrain in xnlGrains)
					{
						string grain = xnGrain.Attributes["name"].Value;
						string votes = xnGrain.Attributes["votes"].Value;
						
						ponderInput(contest, choice, party, grain, votes);
					}
				}

				// Having seen it, now not needed
				//
				//{
				//	// 2. Residual votes
				//	// <VoteType name="Undervotes"...
				//	XmlNodeList xnlGrains = xnContest.SelectNodes("VoteType[@name='Overvotes' or @name='Undervotes']/" + GrainTag);
				//	foreach(XmlNode xnGrain in xnlGrains)
				//	{
				//		string grain = xnGrain.Attributes["name"].Value;
				//		string votes = xnGrain.Attributes["votes"].Value;
				//
				//		ponderInput(contest, "Residual", "", grain, votes);
				//	}
				//}
			}
			// Wow. That was way easier.
		}

...................................................................*/

		
		static void ponderInput(string contest, string choice, string party, string grain, string votes)
		{
			// say(contest + "::" + choice + "::" + party + "::" + grain + "::" + votes);
			
			int nVotes = int.Parse(votes);
			
			// Hang novel grain names into grains dict as encountered.
			
			Grain thisGrain = null;
			if(grains.ContainsKey(grain))
				thisGrain = grains[grain];
			else
				grains.Add(grain, thisGrain = new Grain(grain));
			
			// Hang novel contest names into contests sub-dict as encountered.

			Contest thisContest = null;
			if(thisGrain.contests.ContainsKey(contest))
				thisContest = thisGrain.contests[contest];
			else
				thisGrain.contests.Add(contest, thisContest = new Contest());

			// Hang novel choice names into choices sub-sub-dict as encountered.
			
			// I save the REP, DEM, ETC into choice, but need no sub-branches.
			
			Choice thisChoice = null;
			if(thisContest.choices.ContainsKey(choice))
				thisChoice = thisContest.choices[choice];
			else
				thisContest.choices.Add(choice, thisChoice = new Choice(party));
			
			// count these votes into the hanging grain-contest-choice(party)

			thisChoice.votes += nVotes;
		}

		
		/*
		 * I think having made the effort to parse and count Residual counts,
		 * all contests->...->grains->votes should sum to the identical ballotsCast.
		 * 
		 * <VoterTurnout totalVoters="1035172" ballotsCast="775379" voterTurnout="74.90">
		 * 
		 * No, I think not now:
		 * Some local races have fewer of the statewide ballots,
		 * but I did get some lines to sum up within a Grain:
		 * 
		 * input, under TotalVotes:
		 * <Precinct name="Addison Township, Precinct 1" totalVoters="1930" ballotsCast="1590" ...
		 * 
		 * versus output:
		 * Addison Township, Precinct 1::Straight Party Ticket::1590
		 * Addison Township, Precinct 1::Electors of President and Vice-President of the United States::1590
		 * Addison Township, Precinct 1::United States Senator::1590
		 * 
		 * however, I see some near-multiples and multiples too:
		 * Addison Township, Precinct 1::Member of the State Board of Education::3179
		 * Addison Township, Precinct 1::Regent of the University of Michigan::3180
		 * 
		 * So I think my diagnostic failed to sum-up to a high enough level...
		 * 
		 * Fixed it.
		 * 
		 * Did not snag all of Federals; Many others are don't cares:
		 * GOOD TOTAL: Straight Party Ticket
		 * GOOD TOTAL: Electors of President and Vice-President of the United States
		 * GOOD TOTAL: United States Senator
		 * hand work:
		 * Line 1: Representative in Congress 8th District = 164088
		 * Line 26: Representative in Congress 9th District = 137999
		 * Line 49: Representative in Congress 11th District = 293394
		 * Line 156: Representative in Congress 14th District = 179898
		 * 164088 + 137999 + 293394  + 179898 = 775379, matches ballotsCast = 775379
		 */

		
		static void diagnosticDump()
		{
			// because of the inverted order of grain<-->contest, sum up:
			Dictionary<string,int> contestSums = new Dictionary<string, int>();

			foreach(KeyValuePair<string,Grain>kvp1 in grains)
			{
				string g = kvp1.Key;
				Grain grain = kvp1.Value;
				foreach(KeyValuePair<string,Contest>kvp2 in grain.contests)
				{
					string c = kvp2.Key;
					Contest contest = kvp2.Value;
					int sumVotes = 0;
					foreach(KeyValuePair<string,Choice>kvp3 in contest.choices)
					{
						string n = kvp3.Key;
						Choice choice = kvp3.Value;
						sumVotes += choice.votes;
					}
					if(contestSums.ContainsKey(c))
						contestSums[c] += sumVotes;
					else
						contestSums.Add(c, sumVotes);
				}
			}
			
			//int nGood = 0;
			//int nBad = 0;
			//foreach(KeyValuePair<string,int>kvp in contestSums)
			//{
			//	if(kvp.Value == ballotsCast)
			//	{
			//		// say("GOOD TOTAL: " + kvp.Key); // see which 15?
			//		nGood++;
			//	}
			//	else
			//	{
			//		// say(kvp.Key + " = " + kvp.Value); // How bad?
			//		nBad++;
			//	}
			//}
			// say("nGood = " + nGood);
			// say("nBad = " + nBad);
			//nGood = 15
			//nBad = 260
			// I'll take that as a big win
		}
		

		// This will collect output lines to sort for csv.
		// After sort, re-parse column data to draw graph.
		
		static List<string> csvLines = new List<string>();

		
		// My God! Thank You, My God!
		// The Excel plot reveals a VERY STRAIGHT LINE
		// revealing the algorithm favoring Joe Biden.
		// So that I must see and report mean, std dev.
		//
		// That 1.5% was in the OaklandCountyMI data.
		// for generality, output both statistics.
		//
		// (But MilwaukeeCountyWI had a sloping line.)
		// But output these as a signed percentage:

		// These hold the counts for mean, std dev:
		
		static List<int> BidenBonuses = new List<int>();
		static List<int> TrumpBonuses = new List<int>();
		
		
		// Isaiah 41:15 “Behold, I will make thee a new sharp threshing instrument having teeth: thou shalt thresh the mountains, and beat them small, and shalt make the hills as chaff.”
		
		
		static void LetsRaceTowardTheDesideratum()
		{
			StringBuilder sb = new StringBuilder();
			
			// Grains are plotted along the Abscissa, the X axis.
			// Each outer loop can prepare one csv/plot data out.
			// That is because I already learned the ballotsCast.
			// No rather, compute a total ballots for each grain.

			// I will learn contest long before I learn party.

			// In Version one, that is, before 2020-11-27,
			// I wrongly placed the 'thisFour' ahead of loop.
			// In Correct version two, on/after 2020-11-28,
			// I moved the 'thisFour' creation into the loop.
			
			foreach(KeyValuePair<string,Grain>kvp1 in grains)
			{

				
				ShrodingersCat [] thisFour = new ShrodingersCat[4];

				thisFour[0] = new ShrodingersCat(); // Straight -- no effect if not applicable
				thisFour[1] = new ShrodingersCat(); // President
				thisFour[2] = new ShrodingersCat(); // Senate
				thisFour[3] = new ShrodingersCat(); // Congress

				int which = 0;


				string g = kvp1.Key; // county, ward, precinct name
				Grain grain = kvp1.Value;

				// Middle loop splits up 2 contest types: (POTUS vs. LOWER)
				// Well, actually, into the four cats I created just above.

				foreach(KeyValuePair<string,Contest>kvp2 in grain.contests)
				{
					string c = kvp2.Key;
					Contest contest = kvp2.Value;

					switch(c)
					{
						case "Straight Party Ticket":
							// I should add to all possible races.
							// These will only dilute the symptom.
							which = 0;
							break;

						case "President/Vice-President (Vote for 1)":
							which = 1;
							break;

						case "United States Senator (Vote for 1)":
							which = 2;
							break;

						case "Representative in Congress 14th District (Vote for 1)":
						case "Representative in Congress 13th District (Vote for 1)":
						case "Representative in Congress 12th District (Wayne County pcts only) (Vote for 1)":
						case "Representative in Congress 11th District (Wayne County pcts only) (Vote for 1)":
							which = 3;
							break;
					}

					// Inner loop splits up 2 choice types: (REP vs. DEM).
					
					// In Oakland County MI data, these came as XML attributes:

					foreach(KeyValuePair<string,Choice>kvp3 in contest.choices)
					{
						string n = kvp3.Key;
						Choice choice = kvp3.Value;
						
						switch(choice.party)
						{
							case "DEM":
								thisFour[which].dem += choice.votes;
								break;
							case "REP":
								thisFour[which].rep += choice.votes;
								break;
							default:
								thisFour[which].etc += choice.votes;
								break;
						}
					}
				}

				// build a CSV output line for this grain
				
				
				// fyi: csv("ordering,ppmRepPotus,ppmDemPotus,1M-ppmDemPotus,ppmRepOther,ppmDemOther,1M-ppmDemOther,BonusToTrump,BonusToBiden,totalBallots,locality");

				// sum up VOTES:
				int RepPotus = thisFour[0].rep + thisFour[1].rep;
				int DemPotus = thisFour[0].dem + thisFour[1].dem;
				int EtcPotus = thisFour[0].etc + thisFour[1].etc;
				
				// In Wayne County Michigan, there was an "undervote"
				// i.e., apparent bonus of about 1-2% for both sides.
				
				// BTW, CISA rumor control says that that is normal:
				// https://www.cisa.gov/rumorcontrol
				
				// But in this case, I summed EtcPotus into totalBallots,
				// whereas the sum for Other races sums only Rep and Dem.
				
				// therefore, optionally, omitEtcPotusFromTotalBallots.
				
				int totalBallots = RepPotus + DemPotus + EtcPotus;
				if(omitEtcPotusFromTotalBallots)
				{
					totalBallots = RepPotus + DemPotus; // omit  + EtcPotus
					
				}

				// prevent divide by zero:
				// also declutter noise.
				if(totalBallots < 1 + someMinimumTotalBallots)
				{
					say("OMITTING where totalBallots == 0 in " + g);
				}
				else
				{
					// 'Other' meaning non-POTUS
					
					int RepOther = -1;
					int DemOther = -1;
					switch(howOther) // 1=average of Sen+Rep, 2=Sen, 3=Rep
					{
						case 1:
							RepOther = thisFour[0].rep + (thisFour[2].rep + thisFour[3].rep) / 2;
							DemOther = thisFour[0].dem + (thisFour[2].dem + thisFour[3].dem) / 2;
							break;
						case 2:
							RepOther = thisFour[0].rep + thisFour[2].rep;
							DemOther = thisFour[0].dem + thisFour[2].dem;
							break;
						case 3:
							RepOther = thisFour[0].rep + thisFour[3].rep;
							DemOther = thisFour[0].dem + thisFour[3].dem;
							break;
					}
					
					int ppmRepPotus = (int)(1000000L * RepPotus / totalBallots);
					int ppmDemPotus = (int)(1000000L * DemPotus / totalBallots);

					int ppmRepOther = (int)(1000000L * RepOther / totalBallots);
					int ppmDemOther = (int)(1000000L * DemOther / totalBallots);
					
					int ppmTrumpBonus = ppmRepPotus - ppmRepOther;
					int ppmBidenBonus = ppmDemPotus - ppmDemOther;
					
					
					// Chop off the left of graph,
					// when no clear fraud effect.
					if(ppmRepPotus < someMinimumRepublicanism)
					{
						say("OMITTING where ppmRepPotus < someMinimumRepublicanism in " + g);
					}
					else
					{
						SumOfGrainTotalBallots += totalBallots;

						// these are signed, around zero, but scaled in PPM

						BidenBonuses.Add(ppmBidenBonus); // for later mean, std dev
						TrumpBonuses.Add(ppmTrumpBonus); // for later mean, std dev

						
						int bonusToTrump = 500000 + scaleFactor * ppmTrumpBonus; // both plot up==positive
						int bonusToBiden = 500000 + scaleFactor * ppmBidenBonus; // both plot up==positive
						
						// This ordering method follows (only Other, of only Rep) no Dem, no Potus,
						// as Original Milwaukee article says shifts proportional to Republicanism.
						
						int ppmOrdering = ppmRepOther;
						
						
						// These make valuable alternate plots for visualization.
						
						switch(howOrdering)
						{
							case 1:
								{
									ppmOrdering = bonusToBiden;
									orderingPrinciple = "ascending BonusToBiden";
								}
								break;
							case 2:
								{
									ppmOrdering = bonusToTrump;
									orderingPrinciple = "ascending BonusToTrump";
								}
								break;
							case 3:
								{
									ppmOrdering = ppmDemOther;
									orderingPrinciple = "ascending ppmDemOther";
								}
								break;
							case 4:
								{
									ppmOrdering = ppmRepOther;
									orderingPrinciple = "ascending ppmRepOther";
								}
								break;
							case 5:
								{
									ppmOrdering = ppmDemPotus;
									orderingPrinciple = "ascending ppmDemPotus";
								}
								break;
							case 6:
								{
									ppmOrdering = ppmRepPotus;
									orderingPrinciple = "ascending ppmRepPotus";
								}
								break;
							case 7:
								{
									ppmOrdering = totalBallots;
									orderingPrinciple = "ascending totalBallots";
								}
								break;
						}

						// that's the data, create the CSV line

						
						sb.Clear();
						// field[0]
						sb.Append(ppmOrdering.ToString().PadLeft(7)); sb.Append(',');

						// field[1]
						sb.Append(ppmRepPotus.ToString().PadLeft(7)); sb.Append(',');
						// field[2]
						sb.Append(ppmDemPotus.ToString().PadLeft(7)); sb.Append(',');
						sb.Append((1000000 - ppmDemPotus).ToString().PadLeft(7)); sb.Append(',');

						// field[4]
						sb.Append(ppmRepOther.ToString().PadLeft(7)); sb.Append(',');
						// field[5]
						sb.Append(ppmDemOther.ToString().PadLeft(7)); sb.Append(',');
						sb.Append((1000000 - ppmDemOther).ToString().PadLeft(7)); sb.Append(',');

						// field[7]
						sb.Append(bonusToTrump.ToString().PadLeft(7)); sb.Append(',');
						// field[8]
						sb.Append(bonusToBiden.ToString().PadLeft(7)); sb.Append(',');

						sb.Append(totalBallots.ToString().PadLeft(7)); sb.Append(',');

						sb.Append(regexNonAlnum.Replace(grain.locality, ""));

						csvLines.Add(sb.ToString());
					}
				}
			}
		}
		
		static void OutputTheCsvResults()
		{
			csvLines.Sort();
			
			foreach(string line in csvLines)
				csv(line);
		}
		
		static void OutputTheStatistics()
		{
			// this was more relevant when my 'four' error masked big Bonus variations.
			string possibleConclusionText = "";
			
			// Do once for Biden
			{
				int sum = 0;
				foreach(int i in BidenBonuses) // signed around zero, scaled*1M
					sum += i;
				double mean = (double)sum / BidenBonuses.Count;
				
				double sumSquares = 0.0;
				foreach(int i in BidenBonuses)
					sumSquares += (mean-i)*(mean-i);
				double stdDev = Math.Sqrt(sumSquares / BidenBonuses.Count);
				
				// divide by 100 first to keep 2 dec plcs in pct
				int imean = (int)Math.Round(mean/100);
				string sign = (imean<0?"MINUS ":"PLUS ");
				imean = Math.Abs(imean);
				int istdDev = (int)Math.Round(stdDev/100);
				double dmean = imean / 100.0; // now as Percentage
				double dstdDev = istdDev / 100.0; // now as Percentage

				say("Across the " + BidenBonuses.Count + " localities of " + LocationBeingStudied
				    + ", the BIDEN vote differs from other DEMOCRAT races as: mean = "
				    + sign + dmean + "% of total votes in locality, with standard deviation = " + dstdDev + "%.");
				possibleConclusionText += " BIDEN BONUS = " + sign + dmean + "%, StdDev " + dstdDev + ";";
			}
			
			// Do once for Trump
			{
				int sum = 0;
				foreach(int i in TrumpBonuses) // signed around zero, scaled*1M
					sum += i;
				double mean = (double)sum / TrumpBonuses.Count;
				
				double sumSquares = 0.0;
				foreach(int i in TrumpBonuses)
					sumSquares += (mean-i)*(mean-i);
				double stdDev = Math.Sqrt(sumSquares / TrumpBonuses.Count);
				
				// divide by 100 first to keep 2 dec plcs in pct
				int imean = (int)Math.Round(mean/100);
				string sign = (imean<0?"MINUS ":"PLUS ");
				imean = Math.Abs(imean);
				int istdDev = (int)Math.Round(stdDev/100);
				double dmean = imean / 100.0; // now as Percentage
				double dstdDev = istdDev / 100.0; // now as Percentage
				
				say("Across the " + TrumpBonuses.Count + " localities of " + LocationBeingStudied
				    + ", the TRUMP vote differs from other REPUBLICAN races as: mean = "
				    + sign + dmean + "% of total votes in locality, with standard deviation = " + dstdDev + "%.");
				possibleConclusionText += " TRUMP BONUS = " + sign + dmean + "%, StdDev " + dstdDev;
			}
			say("possibleConclusionText" + possibleConclusionText);
		}
		
		
		// Habakkuk 2:2 "And the Lord answered me, and said, Write the vision, and make it plain upon tables, that he may run that readeth it."

		
		static void OutputThePlotResults()
		{
			Bitmap bmp = new Bitmap(imageSizeX, imageSizeY);
			bmp.SetResolution(92, 92);
			Graphics gBmp = Graphics.FromImage(bmp);
			
			Brush whiteBrush = new SolidBrush(Color.White);
			gBmp.FillRectangle(whiteBrush, 0, 0, imageSizeX, imageSizeY); // x,y,w,h

			// again:csv("ordering,ppmRepPotus,ppmDemPotus,1M-ppmDemPotus,ppmRepOther,ppmDemOther,1M-ppmDemOther,BonusToTrump,BonusToBiden,totalBallots,locality");
			
			// goggled the official party colors.
			// Rep Red
			//RGB: 233 20 29
			//HSL: 357° 84% 50%
			//Hex: #E9141D
			// Dem Blue
			//RGB: 0 21 188
			//HSL: 233° 100% 37%
			//Hex: #0015BC

			const int votesLineWidth = 12; // at 1920 * 2
			Pen redTrumpLinePen = new Pen(Color.FromArgb(233, 20, 29), votesLineWidth);
			Pen blueBidenLinePen = new Pen(Color.FromArgb(0, 21, 188), votesLineWidth);

			const int bonusLineWidth = 8; // at 1920 * 2
			Pen GreenBidenBonusLinePen = new Pen(Color.Green, bonusLineWidth);
			Pen orangeTrumpBonusLinePen = new Pen(Color.Orange, bonusLineWidth);

			Pen blackGraticulePen = new Pen(Color.Black, 6); // at 1920* 2
			Pen blackFatCenterLinePen = new Pen(Color.Black, 16); // at 1920* 2

			// Wiki: Pastels in HSV have high value and low to intermediate saturation.
			// Made my color conversions at https://serennu.com/colour/hsltorgb.php
			//Trying Pastel HSL: 357 40 90
			//HSL: 357° 40% 90%
			//RGB: 240 219 220
			//Hex: #F0DBDC
			//Trying Pastel HSL: 233 40 90
			//HSL: 233° 40% 90%
			//RGB: 219 222 240
			//Hex: #DBDEF0
			
			Brush redRepublicansBrush = new SolidBrush(Color.FromArgb(240, 219, 220));
			Brush blueDemocratsBrush = new SolidBrush(Color.FromArgb(219, 222, 240));

			// there are two similar looking code blocks, for areas, and for lines.

			// plot the areas

			{
				// gBmp.FillRectangle(); // brush,x,y,w,h

				// SumOfGrainTotalBallots must fit inside plotSizeX.
				// The ballots in grain is last-1 field of csv data.
				
				// ascends during loops
				float leftOfBar = nBorder;

				// factor in the border offsets ahead of loop
				int zeroForDescendingY = nBorder;

				//int zeroForBonusesY = (1 + 5*plotSizeY/10) + nBorder;
				int zeroForAscendingY = plotSizeY + nBorder;

				
				// draw areas first
				
				foreach(string line in csvLines)
				{
					string[] fields = line.Split(caComma);
					int grainBallots = int.Parse(fields[9]);
					float barWidth = (float)plotSizeX * grainBallots / SumOfGrainTotalBallots;

					int ppmRepOther = int.Parse(fields[4]);
					int ppmDemOther = int.Parse(fields[5]);
					float RepHeight = (float)plotSizeY * ppmRepOther / 1000000;
					float DemHeight = (float)plotSizeY * ppmDemOther / 1000000;
					gBmp.FillRectangle(blueDemocratsBrush, leftOfBar, zeroForDescendingY, barWidth, DemHeight);
					gBmp.FillRectangle(redRepublicansBrush, leftOfBar, zeroForAscendingY - RepHeight, barWidth, RepHeight);
					
					leftOfBar += barWidth;
				}
			}

			// draw graticules atop areas

			{
				int gratStepSize;
				if(do21gratsfspm10)
					gratStepSize = plotSizeY/20;
				else
					gratStepSize = plotSizeY/10;

				// Draw the eleven 10% or 21 5% horizontal graticule lines:
				for(int y = 1; y <= plotSizeY; y += gratStepSize)
				{
					if(y == 1 + 5*plotSizeY/10)
						gBmp.DrawLine(blackFatCenterLinePen, nBorder, nBorder + y, nBorder + plotSizeX, nBorder + y); // x1, y1, x2, y2
					else
						gBmp.DrawLine(blackGraticulePen, nBorder, nBorder + y, nBorder + plotSizeX, nBorder + y); // x1, y1, x2, y2
				}

				// Draw two vertical boundary lines
				for(int x = 0; x <= plotSizeX; x += plotSizeX)
				{
					// looked like high water trouser legs. Fudging + 1:
					gBmp.DrawLine(blackGraticulePen, nBorder + x, nBorder, nBorder + x, nBorder + plotSizeY + 1); // x1, y1, x2, y2
				}
			}
			
			// plot the varying data lines
			
			{
				// ascends during loops
				float leftOfBar = nBorder;

				// factor in the border offsets ahead of loop
				int zeroForDescendingY = nBorder;
				int zeroForBonusesY = (1 + 5*plotSizeY/10) + nBorder;
				int zeroForAscendingY = plotSizeY + nBorder;

				// these are to connect line segments
				float priorXreached = 0;
				float priorTrumpY = 0;
				float priorBidenY = 0;
				float priorTrumpBonusY = 0;
				float priorBidenBonusY = 0;
				
				foreach(string line in csvLines)
				{
					string[] fields = line.Split(caComma);
					int grainBallots = int.Parse(fields[9]);
					float barWidth = (float)plotSizeX * grainBallots / SumOfGrainTotalBallots;

					int ppmRepPotus = int.Parse(fields[1]);
					int ppmDemPotus = int.Parse(fields[2]);
					int bonusToTrump = int.Parse(fields[7]) - 500000; // was scaled up *10 and shifted +500K
					int bonusToBiden = int.Parse(fields[8]) - 500000; // was scaled up *10 and shifted +500K

					float RepHeight = (float)plotSizeY * ppmRepPotus / 1000000;
					float DemHeight = (float)plotSizeY * ppmDemPotus / 1000000;

					float trumpY = zeroForAscendingY - RepHeight;
					float bidenY = zeroForDescendingY + DemHeight;

					// minus here, as +bonus plots upward, bu Windows GDI +y is downward:
					float trumpBonusY = zeroForBonusesY - (float)plotSizeY * bonusToTrump / 1000000;
					float bidenBonusY = zeroForBonusesY - (float)plotSizeY * bonusToBiden / 1000000;

					float midBarX1 = leftOfBar + barWidth * 0.1f; // misnomer now.
					float midBarX2 = leftOfBar + barWidth * 0.9f; // second point.

					if(priorXreached != 0)
					{
						// connect the prior line segments to these new ones.
						
						// draw bonuses first
						gBmp.DrawLine(GreenBidenBonusLinePen, priorXreached, priorBidenBonusY, midBarX1, bidenBonusY); // pen, x1, y1, x2, y2
						gBmp.DrawLine(orangeTrumpBonusLinePen, priorXreached, priorTrumpBonusY, midBarX1, trumpBonusY); // pen, x1, y1, x2, y2

						// draw votes second on top
						gBmp.DrawLine(blueBidenLinePen, priorXreached, priorBidenY, midBarX1, bidenY); // pen, x1, y1, x2, y2
						gBmp.DrawLine(redTrumpLinePen, priorXreached, priorTrumpY, midBarX1, trumpY); // pen, x1, y1, x2, y2
					}
					
					{
						// always then draw a flat top/bottom line within grain bar.
						
						// draw bonuses first
						gBmp.DrawLine(GreenBidenBonusLinePen, midBarX1, bidenBonusY, midBarX2, bidenBonusY); // pen, x1, y1, x2, y2
						gBmp.DrawLine(orangeTrumpBonusLinePen, midBarX1, trumpBonusY, midBarX2, trumpBonusY); // pen, x1, y1, x2, y2

						// draw votes second on top
						gBmp.DrawLine(blueBidenLinePen, midBarX1, bidenY, midBarX2, bidenY); // pen, x1, y1, x2, y2
						gBmp.DrawLine(redTrumpLinePen, midBarX1, trumpY, midBarX2, trumpY); // pen, x1, y1, x2, y2
					}
					
					priorXreached = midBarX2;
					priorTrumpY = trumpY;
					priorBidenY = bidenY;
					priorTrumpBonusY = trumpBonusY;
					priorBidenBonusY = bidenBonusY;

					// Save this X to tie a bow on it for Pennsylvania
					
					// Special, to tie a bow on the results for Wayne County
					// Draw a special final line and legend to highlight big steal.

					// I am in too big a hurry to think clearly.
					// just do it in terms of the plot numbers.
					// Remember, Y is increasing downward:
					
					// These scalled-to-ppm bonuses are still +/- around ZERO here:
					
					if(bonusToTrump < -XMarksTheSpotThreshold
					   && bonusToBiden > XMarksTheSpotThreshold)
					{
						countTheXMarksTheSpot ++;
						
						// The faster I run, the behinder I get!

						// go back to the CSV data, where 1M = 10% Full Scale.
						// Actually, here was the computation: 10% FS, or 20% FS:
						//int ppmTrumpBonus = ppmRepPotus - ppmRepOther;
						//int ppmBidenBonus = ppmDemPotus - ppmDemOther;
						//int scaleFactor = (do21gratsfspm10 ? 5 : 10);
						//int bonusToTrump = 500000 + scaleFactor * ppmTrumpBonus; // both plot up==positive
						//int bonusToBiden = 500000 + scaleFactor * ppmBidenBonus; // both plot up==positive

						// My head hurts. Reverse that computation:
						// Only 3K votes cannot be right!
						// Whoops, stop using integers here
						int ppmBonusToTrump = (int)((int.Parse(fields[7]) - 500000) / (double)scaleFactor);
						int ppmBonusToBiden = (int)((int.Parse(fields[8]) - 500000) / (double)scaleFactor);
						// still in scope: int grainBallots = int.Parse(fields[9]);
						int netShiftOfVotes = (ppmBonusToBiden-ppmBonusToTrump) * grainBallots / 1000000;
						
						// No it was not that, the numbers ARE really small.
						// Summing the 275 rows in Excel gives only 362,558 total votes
						// So 3393 votes is almost 1%.

						effectOnVotes += netShiftOfVotes;
						
						// give me a vertical to emphasize the two bonuses.
						// Actually, draw an X to mark the spot:
						// Fat pen is pretty to hold up, but covers the detail
						gBmp.DrawLine(blackGraticulePen, midBarX1, bidenBonusY, midBarX2, trumpBonusY); // pen, x1, y1, x2, y2
						gBmp.DrawLine(blackGraticulePen, midBarX2, bidenBonusY, midBarX1, trumpBonusY); // pen, x1, y1, x2, y2
						
						// mention where it happened
						say("MARKING AN X ON [" + fields[10] + "] for shifting " + netShiftOfVotes + " votes.");
					}
					
					leftOfBar += barWidth;
				}
			}
			
			// draw the legends

			{
				// Draw most text in black
				Brush blackTextBrush = new SolidBrush(Color.Black);

				// Republican RED looks nice for this bold need:
				Brush conclusionRedTextBrush = new SolidBrush(Color.FromArgb(233, 20, 29));
				
				Font bigFont = new Font("Arial Black", 80);
				int bigFontHeight = (int)bigFont.GetHeight(gBmp);

				Font smallFont = new Font("Arial Black", 40);
				int smallFontHeight = (int)smallFont.GetHeight(gBmp);

				// Tweak this for the maximum width that fits
				Font conclusionFont = new Font("Arial Black", 52);
				int conclusionFontHeight = (int)conclusionFont.GetHeight(gBmp);

				Font unambiguousFont = new Font("Consolas", 35);
				int unambiguousFontHeight = (int)smallFont.GetHeight(gBmp);

				
				// above plot
				
				int yHeadine = nBorder / 2 - bigFontHeight / 2;
				string Headine = "Analyzing election data for " + LocationBeingStudiedClearly;
				gBmp.DrawString(Headine, bigFont, blackTextBrush, (imageSizeX-gBmp.MeasureString(Headine, bigFont).Width) / 2, yHeadine);

				
				// out of 1000, all 50's fall on graticules. 25 / 75 are safe.


				// MAKING THE CONCLUSION MORE SPECTACULAR, AND RED!
				// 25 would be top row, but lands on PA Bucks Biden Bonus

				finalConclusion = "X MARKS " + countTheXMarksTheSpot + " SPOTS OF THEFT WORTH " + effectOnVotes + " VOTES, OR 1% OF ALL VOTES. SEE LOG.";
				say("finalConclusion = " + finalConclusion);
				int YQEDLabel = nBorder + 225 * plotSizeY / 1000 - conclusionFont.Height / 2;
				string QEDLabel = finalConclusion;
				gBmp.DrawString(QEDLabel, conclusionFont, conclusionRedTextBrush, (imageSizeX-gBmp.MeasureString(QEDLabel, conclusionFont).Width) / 2, YQEDLabel);

				
				int yMeaningLabel = nBorder + 75 * plotSizeY / 1000 - smallFontHeight / 2;
				string howDone = "INVALID";
				switch(howOther)
				{
					case 1:
						// howDone = "average SENATE & CONGRESS"; // too long
						howDone = "average SENATE+HOUSE";
						break;
					case 2:
						howDone = "SENATE";
						break;
					case 3:
						howDone = "CONGRESS";
						break;
				}

				string MeaningLabel = "Republicans are RED, Democrats are BLUE; Lines show % POTUS votes; Areas show % " + howDone + " votes.";
				gBmp.DrawString(MeaningLabel, smallFont, blackTextBrush, (imageSizeX-gBmp.MeasureString(MeaningLabel, smallFont).Width) / 2, yMeaningLabel);

				
				int yFraudLabel = nBorder + 125 * plotSizeY / 1000 - smallFontHeight / 2;
				string FraudLabel = "ELECTION FRAUD CLUE WHENEVER RED AND/OR BLUE LINES SYSTEMATICALLY DO NOT TRACK THEIR AREA EDGE.";
				gBmp.DrawString(FraudLabel, smallFont, blackTextBrush, (imageSizeX-gBmp.MeasureString(FraudLabel, smallFont).Width) / 2, yFraudLabel);

				
				string fullScale = (do21gratsfspm10? "Full scale = +/-10%": "Full scale = +/-5%");

				int YBidenLabel = nBorder + 875 * plotSizeY / 1000 - smallFontHeight / 2;
				string BidenLabel = "The GREEN line amplifies any % Bonus To BIDEN, above (or loss below) fat center graticule. " + fullScale;
				gBmp.DrawString(BidenLabel, smallFont, blackTextBrush, (imageSizeX-gBmp.MeasureString(BidenLabel, smallFont).Width) / 2, YBidenLabel);

				
				int YTrumpLabel = nBorder + 925 * plotSizeY / 1000 - smallFontHeight / 2;
				string TrumpLabel = "The ORANGE line amplifies any % Bonus To TRUMP, above (or loss below) fat center graticule. " + fullScale;
				gBmp.DrawString(TrumpLabel, smallFont, blackTextBrush, (imageSizeX-gBmp.MeasureString(TrumpLabel, smallFont).Width) / 2, YTrumpLabel);

				
				// 975 was my plan, but lands on BUCKS TRUMP MINUS
				int yOrderLabel = nBorder + 825 * plotSizeY / 1000 - smallFontHeight / 2;
				string AboveLabel = "Localities (\"" + GrainTag + "\") are plotted <-- left to right --> by " + orderingPrinciple + ".";
				gBmp.DrawString(AboveLabel, smallFont, blackTextBrush, (imageSizeX-gBmp.MeasureString(AboveLabel, smallFont).Width) / 2, yOrderLabel);

				
				// below plot
				
				int yGitHubLabel = nBorder + 105 * plotSizeY / 100 - smallFontHeight / 2;
				string GitHubLabel = "Open Source: " + gitHubRepositoryShortened + " = " + gitHubRepositoryURL;
				gBmp.DrawString(GitHubLabel, unambiguousFont, blackTextBrush, (imageSizeX-gBmp.MeasureString(GitHubLabel, unambiguousFont).Width) / 2, yGitHubLabel);

				
				int yVersionLabel = nBorder + 108 * plotSizeY / 100 - smallFontHeight / 2;
				string VersionLabel = DateStampPlot;
				gBmp.DrawString(VersionLabel, unambiguousFont, blackTextBrush, (imageSizeX-gBmp.MeasureString(VersionLabel, unambiguousFont).Width) / 2, yVersionLabel);

			}

			bmp.Save(pngFilePath, ImageFormat.Png); // can overwrite old png
		}
		
	}
}
