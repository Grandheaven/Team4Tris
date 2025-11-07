using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Page4_DB : MonoBehaviour
{
    // Connection details
    private string host = "deu.duraka.shop";
    private string port = "4264";
    private string sid = "xe";
    private string userid = "TEAM4";
    private string password = "Team4Tris";

    private string saveisbn;

    static public bool run = false;
    static public bool delete = false;
    static public string datatitle;
    static public string dataauthor;
    static public string datapublisher;
    static public string dataprice;
    static public string dataisbn;

    [Header("BookInformation Object")]
    public GameObject bookinfo;
    public TMP_InputField title;
    public TMP_InputField author;
    public TMP_InputField publisher;
    public TMP_InputField price;
    public TMP_InputField url;
    public RawImage bookPhoto;
    public TMP_InputField isbn;
    public TMP_InputField description;

    [Header("BookModify Object")]
    public GameObject bookinfo2;
    public TMP_InputField title2;
    public TMP_InputField author2;
    public TMP_InputField publisher2;
    public TMP_InputField price2;
    public TMP_InputField url2;
    public RawImage bookPhoto2;
    public TMP_InputField isbn2;
    public TMP_InputField description2;
    public GameObject background1;
    public GameObject background2;
    public GameObject background3;

    [Header("Pop-Up Object")]
    public GameObject successUI;
    public GameObject alertUI;
    public GameObject deleteUI;
    public GameObject failUI;
    public TMP_Text failText;

    void Update()
    {
        if (bookinfo.activeInHierarchy && run)
        {
            BookInformation();
            run = false;
        }
        if (bookinfo2.activeInHierarchy && run)
        {
            BookModify();
            run = false;
        }
        if (delete == true && run == true)
        {
            BookDelete();
            run = false;
        }
    }

    public void BookInformation()
    {
        background1.SetActive(false);
        background2.SetActive(false);
        background3.SetActive(false);

        string connString = $"Data Source=(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL=TCP)(HOST={host})(PORT={port})))(CONNECT_DATA=(SID={sid})));User Id={userid};Password={password};";
        OracleConnection connection = new OracleConnection(connString);
        try
        {
            connection.Open();
            string sql = $"SELECT TITLE, AUTHOR, PUBLISHER, PRICE, URL, BOOK_PHOTO, ISBN, DESCRIPTION FROM BOOK WHERE ISBN = {dataisbn}"; // 실제 테이블 이름으로 변경
            OracleCommand command = new OracleCommand(sql, connection);
            OracleDataReader reader = command.ExecuteReader();
            if (reader.Read())
            {
                title.text = ReadString(reader["TITLE"]);
                author.text = ReadString(reader["AUTHOR"]);
                publisher.text = ReadString(reader["PUBLISHER"]);
                price.text = $"{ReadString(reader["PRICE"]):N0}원";
                url.text = ReadString(reader["URL"]);
                isbn.text = ReadString(reader["ISBN"]);
                description.text = ReadString(reader["DESCRIPTION"]);

                if (reader["BOOK_PHOTO"] != DBNull.Value)
                {
                    byte[] imageData = (byte[])reader["BOOK_PHOTO"];

                    if (imageData != null && imageData.Length > 0)
                    {
                        Texture2D texture = new Texture2D(2, 2);
                        if (texture.LoadImage(imageData))
                        {
                            bookPhoto.texture = texture;
                            bookPhoto.color = Color.white;
                        }
                        else
                        {
                            Debug.LogError("이미지 데이터 로드 실패: byte[]가 유효한 이미지 형식이 아닙니다.");
                            bookPhoto.texture = null;
                        }
                    }
                    else
                    {
                        bookPhoto.texture = null;
                    }
                }
                else
                {
                    bookPhoto.texture = null;
                }
            }
        }
        catch (Exception ex) // DB 연결 실패 시 예외 처리
        {
            Debug.LogError("Database connection failed: " + ex.Message);
        }
        finally
        {
            connection.Close();
            Debug.Log("Database connection closed.");
        }
    }

    public void BookModify()
    {
        background1.SetActive(false);
        background2.SetActive(false);
        background3.SetActive(false);

        string connString = $"Data Source=(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL=TCP)(HOST={host})(PORT={port})))(CONNECT_DATA=(SID={sid})));User Id={userid};Password={password};";
        OracleConnection connection = new OracleConnection(connString);
        try
        {
            connection.Open();
            string sql = $"SELECT TITLE, AUTHOR, PUBLISHER, PRICE, URL, BOOK_PHOTO, ISBN, DESCRIPTION FROM BOOK WHERE ISBN = {dataisbn}"; // 실제 테이블 이름으로 변경
            OracleCommand command = new OracleCommand(sql, connection);
            OracleDataReader reader = command.ExecuteReader();
            if (reader.Read())
            {
                title2.text = ReadString(reader["TITLE"]);
                author2.text = ReadString(reader["AUTHOR"]);
                publisher2.text = ReadString(reader["PUBLISHER"]);
                price2.text = $"{ReadString(reader["PRICE"]):N0}원";
                url2.text = ReadString(reader["URL"]);
                isbn2.text = ReadString(reader["ISBN"]);
                description2.text = ReadString(reader["DESCRIPTION"]);

                if (reader["BOOK_PHOTO"] != DBNull.Value)
                {
                    byte[] imageData = (byte[])reader["BOOK_PHOTO"];

                    if (imageData != null && imageData.Length > 0)
                    {
                        Texture2D texture = new Texture2D(2, 2);
                        if (texture.LoadImage(imageData))
                        {
                            bookPhoto2.texture = texture;
                            bookPhoto2.color = Color.white;
                        }
                        else
                        {
                            Debug.LogError("이미지 데이터 로드 실패: byte[]가 유효한 이미지 형식이 아닙니다.");
                            bookPhoto2.texture = null;
                        }
                    }
                    else
                    {
                        bookPhoto2.texture = null;
                    }
                }
                else
                {
                    bookPhoto2.texture = null;
                }
            }
        }
        catch (Exception ex) // DB 연결 실패 시 예외 처리
        {
            Debug.LogError("Database connection failed: " + ex.Message);
        }
        finally
        {
            connection.Close();
            Debug.Log("Database connection closed.");

            saveisbn = isbn2.text;
        }
    }

    public void BookUpdate()
    {
        string connString = $"Data Source=(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL=TCP)(HOST={host})(PORT={port})))(CONNECT_DATA=(SID={sid})));User Id={userid};Password={password};";
        OracleConnection connection = new OracleConnection(connString);
        try
        {
            connection.Open();
            OracleCommand command = new OracleCommand("UPDATE_BOOK", connection);
            command.CommandType = CommandType.StoredProcedure;

            string sql = $"SELECT BNO FROM BOOK WHERE ISBN = {saveisbn}";
            OracleCommand bno = new OracleCommand(sql, connection);
            OracleDataReader reader = bno.ExecuteReader();

            try
            {
                string price = this.price2.text.Replace(",", "").Replace("원", "");

                byte[] book_photo = null;
                if (this.bookPhoto2.texture != null)
                {
                    Texture2D texture = this.bookPhoto2.texture as Texture2D;
                    book_photo = texture.EncodeToPNG();
                }

                if (reader.Read()) command.Parameters.Add("B_BNO", OracleDbType.Decimal).Value = ReadString(reader["BNO"]);
                command.Parameters.Add("B_TITLE", OracleDbType.NVarchar2).Value = title2.text;
                command.Parameters.Add("B_AUTHOR", OracleDbType.NVarchar2).Value = author2.text;
                command.Parameters.Add("B_PUBLISHER", OracleDbType.NVarchar2).Value = publisher2.text;
                command.Parameters.Add("B_PRICE", OracleDbType.Decimal).Value = (string.IsNullOrEmpty(price)) ? DBNull.Value : price;
                command.Parameters.Add("B_URL", OracleDbType.Clob).Value = (string.IsNullOrEmpty(url2.text)) ? DBNull.Value : url2.text;
                command.Parameters.Add("B_BOOKPHOTO", OracleDbType.Blob).Value = (book_photo == null) ? DBNull.Value : book_photo;
                command.Parameters.Add("B_ISBN", OracleDbType.Decimal).Value = isbn2.text;
                command.Parameters.Add("B_DESCRIPTION", OracleDbType.Clob).Value = (string.IsNullOrEmpty(description2.text)) ? DBNull.Value : description2.text;

                command.ExecuteNonQuery();

                successUI.SetActive(true);
            }
            catch (Exception ex)
            {
                failText.text = "필수 입력사항이 누락되거나 \n중복된 도서 정보가 있습니다.";
                failUI.SetActive(true);
                successUI.SetActive(false);
            }
        }
        catch (Exception ex) // DB 연결 실패 시 예외 처리
        {
            Debug.LogError("Database connection failed: " + ex.Message);
        }
        finally
        {
            connection.Close();
            Debug.Log("Database connection closed.");
        }
    }

    public void BookDelete()
    {
        background1.SetActive(false);
        background2.SetActive(false);
        background3.SetActive(false);

        string connString = $"Data Source=(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL=TCP)(HOST={host})(PORT={port})))(CONNECT_DATA=(SID={sid})));User Id={userid};Password={password};";
        OracleConnection connection = new OracleConnection(connString);

        try
        {
            connection.Open();

            // 1. ISBN을 사용하여 BNO를 조회합니다. (datatitle 등으로 DELETE 하는 것은 위험합니다.)
            string selectBnoSql = $"SELECT BNO FROM BOOK WHERE ISBN = :isbn";
            OracleCommand bnoCommand = new OracleCommand(selectBnoSql, connection);
            bnoCommand.Parameters.Add("isbn", OracleDbType.Decimal).Value = dataisbn;
            object bnoResult = bnoCommand.ExecuteScalar();

            if (bnoResult == null || bnoResult == DBNull.Value)
            {
                failText.text = "삭제할 도서 정보를 찾을 수 없습니다.";
                failUI.SetActive(true);
                return;
            }
            decimal bno = Convert.ToDecimal(bnoResult);

            // 2. 해당 BNO로 RENT 테이블에서 IS_RETURNED가 'N'인 레코드가 있는지 확인합니다.
            string checkRentSql = $"SELECT COUNT(*) FROM RENT WHERE BNO = :bno AND IS_RETURNED = 'N'";
            OracleCommand checkRentCommand = new OracleCommand(checkRentSql, connection);
            checkRentCommand.Parameters.Add("bno", OracleDbType.Decimal).Value = bno;
            int activeRents = Convert.ToInt32(checkRentCommand.ExecuteScalar());

            if (activeRents > 0)
            {
                // 활성 대여(IS_RETURNED = 'N')가 존재하면 삭제 불가
                alertUI.SetActive(true);
                deleteUI.SetActive(false);
                failText.text = "현재 대여 중인 도서는 삭제할 수 없습니다.";
                failUI.SetActive(true);
                return;
            }

            // 3. (옵션) IS_RETURNED가 모두 'Y'이거나 대여 기록이 없으면, RENT 테이블에서 BNO 필드를 NULL로 업데이트 (선택적)
            // RENT 테이블의 BNO를 NULL로 업데이트하는 대신, 해당 RENT 레코드를 삭제하거나 BNO NULL 처리를 생략하는 것이 더 일반적입니다.
            // 하지만 요청에 따라 'BNO를 NULL 값으로 지운 후'를 구현합니다.

            // RENT 테이블의 BNO를 NULL로 설정 (BNO가 NULLABLE이므로 가능)
            string updateRentSql = $"UPDATE RENT SET BNO = NULL WHERE BNO = :bno";
            OracleCommand updateRentCommand = new OracleCommand(updateRentSql, connection);
            updateRentCommand.Parameters.Add("bno", OracleDbType.Decimal).Value = bno;
            updateRentCommand.ExecuteNonQuery();


            // 4. BOOK 테이블에서 도서 삭제
            string deleteBookSql = $"DELETE FROM BOOK WHERE BNO = :bno";
            OracleCommand deleteBookCommand = new OracleCommand(deleteBookSql, connection);
            deleteBookCommand.Parameters.Add("bno", OracleDbType.Decimal).Value = bno;
            deleteBookCommand.ExecuteNonQuery();

            deleteUI.SetActive(true); // 성공 팝업

            
        }
        catch (Exception ex) // DB 연결 또는 기타 예외 처리
        {
            Debug.LogError("Database delete failed: " + ex.Message);
            failText.text = "데이터베이스 오류가 발생했습니다: " + ex.Message;
            failUI.SetActive(true);
            deleteUI.SetActive(false);
            alertUI.SetActive(true);
        }
        finally
        {
            connection.Close();
            Debug.Log("Database connection closed.");
            run = false; // BookDelete 실행 완료
            delete = false; // delete 플래그 초기화
        }
    }

    // 참고: BookDelete()의 맨 위에 있던 public void BookDelete() { ... } 내부에서 
    // 배경 UI 끄는 부분 아래에 Page4_DB.run = false; 와 Page4_DB.delete = false; 를 추가해줘야
    // Update() 루프가 종료될 것입니다.

    private string ReadString(object dbValue)
    {
        return (dbValue == DBNull.Value || dbValue == null) ? string.Empty : dbValue.ToString();
    }

    public void SceneInit()
    {
        Page4 active = new Page4();
        active.Click();
    }
}
