namespace SudokuSolver.Views;

internal interface ISession
{
    XElement GetSessionData();

    static abstract bool ValidateSessionData(XElement root);
}
