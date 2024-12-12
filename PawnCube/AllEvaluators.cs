using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace PawnCube
{
    internal class AllEvaluators
    {
        public List<IBooleanEvaluator> GetBoolean()
        {

            var booleanEvaluators = new List<IBooleanEvaluator>() {
                //new AllFourCornersOccupiedByNonRooksEvaluator(),
                //new CastleWithCheckEvaluator(),
                //new AllPiecesOnSameBoardColorWithAtLeastSevenTotal(),
                //new AnyOppositeSideCastlingGameEvaluator(),
                //new AnyPawnPromotionEvaluator(),
                //new AnyTimeOutEvaluator(),
                //new BishopInACornerEvaluator(),
                //new BishopManualUndoEvaluator(),
                //new BishopMovesSevenSquaresEvaluator(),
                //new BishopMovesSixSquaresEvaluator(),
                //new BishopVsKnightEndgameReachedEvaluator(),
                //new BothKingsWrongSideEvaluator(),
                //new CastleAfterMove20Evaluator(),
                //new CastleAfterMove40Evaluator(),
                //new CheckmateWithAPawnEvaluator(),
                ////new Connect3Evaluator(),
                //new Connect4Evaluator(),
                //new Connect5Evaluator(),
                //new Down4PtsOrMoreMaterialButWins(),
                //new EnPassantCaptureHappensEvaluator(),
                //new EnPassantRefusedEvaluator(),
                //new EverQvsRREndgameEvaluator(),
                //new FirstCaptureIsQueen(),
                //new FirstCaptureIsRook(),
                //new FirstCaptureRIsQueen(),
                //new FourteenPieceGameEndEvaluator(),
                //new FullComplimentOfMinorPiecesSurvivesEvaluator(),
                //new FullFileEvaluator(),
                //new FullRankEvaluator(),
                //new KingInACornerEvaluator(),
                //new KingReachesOpponentsFarSideOfBoardEvaluator(),
                //new KnightAndKingInCenterFourSquaresEvaluator(),
                //new KnightInACornerEvaluator(),
                //new NoCapturesBeforeMove10Evaluator(),
                //new NoCapturesBeforeMove20Evaluator(),
                //new NoCapturesBeforeMove30Evaluator(),
                //new NonQueenPromotionEvaluator(),
                //new PawnPromotionAndMateEvaluator(),
                //new PawnTakesAQueenEvaluator(),
                //new PawnUnderpromotionAndMateEvaluator(),
                //new PieceCubeEvaluator(),
                //new PromoteAndCheckEvaluator(),
                //new QuadrupledPawnEvaluator(),
                //new QueenInACornerEvaluator(),
                //new RookTakesAQueenEvaluator(),
                //new SamePieceMovesEightTimesInARowEvaluator(),
                //new TripledPawnEvaluator(),
                //new TwoBishopsVsTwoKnightsEvaluator(),
                //new TwoKnightsOnSideEdgeEvaluator(),
                //new TwoPawnPromotionsInOneGameEvaluator(),
                //new SinglePieceCaptures19PointsOfMaterialInAGame(),
                // new ThanosSnapEvaluator(),
                // new RingOfFireEvaluator(),
                // new NakedKingEvaluator(),
                // new ChainReactionCastlingEvaluator(), 
                // new RomeoAndJulietEvaluator(),
                // new FortKnoxEvaluator(),
            };
            return booleanEvaluators;
        }

        public List<INumericalEvaluator> GetNumerical()
        {
            var numericalEvaluators = new List<INumericalEvaluator>() {
                //new LargestNxNSquareInAnyPositionInAnyGame(),
                //new NonpawnNonmoversWorthTenPercentEachInTheirBiggestGame(),
                //new PawnPromotionFivePercentEachEvaluator(),
                //new KingTakesQueenFivePercentEachEvaluator(),
                //new PieceOnStartingSquareOnePercentEachEvaluator(),
                //new TotalPawnAdvantageSeen20PercentPerPawnEvaluator(),
                //new BiggestPawnMaterialLeadInAnyGameTwentyPercentPerPawnLeadOnBoardEvaluator(),
                //new Black40VsWhiteMinus10WinEvalutor(),
                //new BlackPlayerEverAheadOnPointsInGameSeriesEvaluator(),
                //new CapturedBishopsFiveCapturedPawnsMinusOneEvaluator(),
                //new HalfPercentForEachMoveInLongestGameEvaluator(),
                //new HomecomingPiecesTenPercentEachEvaluator(),
                //new KnightDirectionNumerical3PercentVerticalMinus4PercentHorizontalEvaluator(),
                //new LongestWinstreakByPlayerTwentyPercentEachEvaluator(),
                //new MoreKnightsSurviveThanBishopsEvaluator(),
                //new OnePercentForEachMoveInLongestGameEvaluator(),
                //new OnePercentPerUnmovedPieceEvaluator(),
                //new OverallWhiteGameEndPointAdvantageTenPercentPerEvaluator(),
                //new Pawn10VsKnightMinus10FirstMoveEvaluator(),
                //new PawnMoveTypesFiveForTwoJumpMinusFiveForOtherFirstMove(),
                //new SevenPercentForEachDrawEvaluator(),
                //new ShortCastleTenPercentVsLongCastleMinusFivePercentEvaluator(),
                //new SurvivingPawnsWorthOneSurvivingKnightBishopRookWorthNegativeTwoEvaluator(),
                //new SurvivingQueen5PercentEachEvaluator(),
                //new TenPercentForEachDrawEvaluator(),
                //new TenPercentForEachPointOfMaterialAdvantageInAnyFinalPositionEvaluator(),
                //new TenPercentForeEachBlackWinEvaluator(),
                //new TenPercentForeEachWhiteWinEvaluator(),
                //new TenPercentForeEachWinEvaluator(),
                //new TenPercentPerResignationEvaluator(),
                //new TwentyPercentForDecisiveMinusTenForOtherwiseEvaluator(),
                //new UnmovedNonPawnTenPercentEachEvaluator(),
                //new FivePercentDeadBishopMinusOnePercentPerDeadPawnFinalPositions(),
                //new TotalWhiteAdvantageAtLastPositionTenPercentForeEachMaterialPoint(),
                // new PieceTourEvaluator(),
                // new KnightCoverageEvaluator(),
                
                };
            return numericalEvaluators;
        }
    }
}
