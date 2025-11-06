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

    static public bool run = false;
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
    public TMP_Text alertText;
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
        }
    }

    public void DataBaseConnection()
    {
        string connString = $"Data Source=(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL=TCP)(HOST={host})(PORT={port})))(CONNECT_DATA=(SID={sid})));User Id={userid};Password={password};";
        OracleConnection connection = new OracleConnection(connString);
        try
        {
            connection.Open();
            OracleCommand command = new OracleCommand("INSERT_BOOK", connection);
            command.CommandType = CommandType.StoredProcedure;

            try
            {
                string price = this.price.text.Replace(",", "").Replace("원", "");

                byte[] book_photo = null;
                if (this.bookPhoto.texture != null)
                {
                    Texture2D texture = this.bookPhoto.texture as Texture2D;
                    book_photo = texture.EncodeToPNG();
                }

                command.Parameters.Add("B_TITLE", OracleDbType.NVarchar2).Value = title.text;
                command.Parameters.Add("B_AUTHOR", OracleDbType.NVarchar2).Value = author.text;
                command.Parameters.Add("B_PUBLISHER", OracleDbType.NVarchar2).Value = publisher.text;
                command.Parameters.Add("B_PRICE", OracleDbType.Decimal).Value = (string.IsNullOrEmpty(price)) ? DBNull.Value : price;
                command.Parameters.Add("B_URL", OracleDbType.Clob).Value = (string.IsNullOrEmpty(url.text)) ? DBNull.Value : url.text;
                command.Parameters.Add("B_BOOKPHOTO", OracleDbType.Blob).Value = (book_photo == null) ? DBNull.Value : book_photo;
                command.Parameters.Add("B_ISBN", OracleDbType.Decimal).Value = isbn.text;
                command.Parameters.Add("B_DESCRIPTION", OracleDbType.Clob).Value = (string.IsNullOrEmpty(description.text)) ? DBNull.Value : description.text;

                command.ExecuteNonQuery();

                successUI.SetActive(true);
            }
            catch (Exception ex)
            {
                failText.text = "필수 입력사항이 누락되거나 \n중복된 도서 정보가 있습니다.";
                failUI.SetActive(true);
            }
        }
        catch (Exception ex)
        {
            failText.text = "DB 연결에 실패했습니다.";
            failUI.SetActive(true);
        }
        finally
        {
            connection.Close();
        }
    }

    private string ReadString(object dbValue)
    {
        return (dbValue == DBNull.Value || dbValue == null) ? string.Empty : dbValue.ToString();
    }
}
