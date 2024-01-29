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
                new AnyOppositeSideCastlingGameEvaluator(),
                new AnyPawnPromotionEvaluator(),
                new AnyTimeOutEvaluator(),
                new BishopInACornerEvaluator(),
                new BishopManualUndoEvaluator(),
                new BishopMovesSevenSquaresEvaluator(),
                new BishopMovesSixSquaresEvaluator(),
                new CastleAfterMove20Evaluator(),
                new CastleAfterMove40Evaluator(),
                new CheckmateWithAPawnEvaluator(),
                new Connect3Evaluator(),
                new Connect4Evaluator(),
                new Connect5Evaluator(),
                new EnPassantCaptureHappensEvaluator(),
                new EnPassantRefusedEvaluator(),
                new EverQvsRREndgameEvaluator(),
                new FirstCaptureIsRook(),
                new FourteenPieceGameEndEvaluator(),
                new FullComplimentOfMinorPiecesSurvivesEvaluator(),
                new FullFileEvaluator(),
                new FullRankEvaluator(),
                new KingInACornerEvaluator(),
                new NoCapturesBeforeMove10Evaluator(),
                new NoCapturesBeforeMove20Evaluator(),
                new NoCapturesBeforeMove30Evaluator(),
                new NonQueenPromotionEvaluator(),
                new PawnTakesAQueenEvaluator(),
                new PromoteAndCheckEvaluator(),
                new QuadrupledPawnEvaluator(),
                new QueenInACornerEvaluator(),
                new RookTakesAQueenEvaluator(),
                new SamePieceMovesEightTimesInARowEvaluator(),
                new TripledPawnEvaluator(),
                new TwoPawnPromotionsInOneGameEvaluator(),
            };
            return booleanEvaluators;
        }

        public List<INumericalEvaluator> GetNumerical()
        {

            var numericalEvaluators = new List<INumericalEvaluator>() {
                //new TwentyPercentPerEnPassantCaptureEvaluator(),
                new LongestWinstreakByPlayerTwentyPercentEachEvaluator(),
                new BiggestPawnMaterialLeadInAnyGameTwentyPercentPerPawnLeadOnBoardEvaluator(),
                new BlackPlayerEverAheadOnPointsInGameSeriesEvaluator(),
                //new PerEn
                new Black40VsWhiteMinus10WinEvalutor(),
                new CapturedBishopsFiveCapturedPawnsMinusOneEvaluator(),
                new HalfPercentForEachMoveInLongestGameEvaluator(),
                new HomecomingPiecesTenPercentEachEvaluator(),
                new KnightDirectionNumerical3PercentVerticalMinus4PercentHorizontalEvaluator(),
                new MoreKnightsSurviveThanBishopsEvaluator(),
                new OnePercentForEachMoveInLongestGameEvaluator(),
                new OnePercentPerUnmovedPieceEvaluator(),
                new Pawn10VsKnightMinus10FirstMoveEvaluator(),
                new PawnMoveTypesFiveForTwoJumpMinusFiveForOtherFirstMove(),
                new SevenPercentForEachDrawEvaluator(),
                new ShortCastleTenPercentVsLongCastleMinusFivePercentEvaluator(),
                new SurvivingPawnsWorthOneSurvivingKnightBishopRookWorthNegativeTwoEvaluator(),
                new SurvivingQueen5PercentEachEvaluator(),
                new TenPercentForEachDrawEvaluator(),
                new TenPercentForEachPointOfMaterialAdvantageInFinalPositionEvaluator(),
                new TenPercentForeEachBlackWinEvaluator(),
                new TenPercentForeEachWinEvaluator(),
                new TenPercentPerResignationEvaluator(),
                new TwentyPercentForDecisiveMinusTenForOtherwiseEvaluator(),
                };
            return numericalEvaluators;
        }
    }
}
