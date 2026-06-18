Option Explicit

Public Sub BuildRiskDashboard()
    On Error GoTo ErrHandler

    Dim wb As Workbook
    Dim wsRaw As Worksheet
    Dim wsSummary As Worksheet
    Dim lastRow As Long
    Dim lastCol As Long

    Set wb = ThisWorkbook
    Set wsRaw = wb.Worksheets("RAW_DATA")

    Application.ScreenUpdating = False
    Application.EnableEvents = False
    Application.Calculation = xlCalculationManual

    lastRow = wsRaw.Cells(wsRaw.Rows.Count, 1).End(xlUp).Row
    lastCol = wsRaw.Cells(1, wsRaw.Columns.Count).End(xlToLeft).Column

    If lastRow < 2 Then
        Err.Raise vbObjectError + 100, , "RAW_DATA에 분석할 데이터가 없습니다."
    End If

    Set wsSummary = PrepareSheet(wb, "SUMMARY")
    wsSummary.Range("A1").Value = "리스크관리 대시보드"
    wsSummary.Range("A3").Value = "생성일시"
    wsSummary.Range("B3").Value = Format$(Now, "yyyy-mm-dd hh:nn:ss")
    wsSummary.Range("A4").Value = "원본 행 수"
    wsSummary.Range("B4").Value = lastRow - 1

CleanExit:
    Application.ScreenUpdating = True
    Application.EnableEvents = True
    Application.Calculation = xlCalculationAutomatic
    Exit Sub

ErrHandler:
    MsgBox "대시보드 생성 중 오류가 발생했습니다." & vbCrLf & _
           "오류번호: " & Err.Number & vbCrLf & _
           "오류내용: " & Err.Description, vbCritical
    Resume CleanExit
End Sub

Private Function PrepareSheet(ByVal wb As Workbook, ByVal sheetName As String) As Worksheet
    Dim ws As Worksheet

    On Error Resume Next
    Set ws = wb.Worksheets(sheetName)
    On Error GoTo 0

    If ws Is Nothing Then
        Set ws = wb.Worksheets.Add(After:=wb.Worksheets(wb.Worksheets.Count))
        ws.Name = sheetName
    Else
        ws.Cells.Clear
    End If

    Set PrepareSheet = ws
End Function
