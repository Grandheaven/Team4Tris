using UnityEngine;
using System;
using System.Data;
using System.Collections;
using System.Collections.Generic;
using Oracle.ManagedDataAccess.Client;
using TMPro;
using UnityEngine.UI;

public class Page3_DB : MonoBehaviour
{
    // Connection details
    private string host = "deu.duraka.shop";
    private string port = "4264";
    private string sid = "xe";
    private string userid = "TEAM4";
    private string password = "Team4Tris";

    [Header("Input Object")]
    public TMP_InputField title;
    public TMP_InputField author;
    public TMP_InputField publisher;
    public TMP_InputField price;
    public TMP_InputField url;
    public RawImage bookPhoto;
    public TMP_InputField isbn;
    public RawImage isbnPhoto;
    public TMP_InputField description;

    [Header("Pop-Up Object")]
    public GameObject successUI;
    public GameObject failUI;
    public TMP_Text failText;

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
                byte[] book_photo = null;
                if (this.bookPhoto.texture != null)
                {
                    Texture2D texture = this.bookPhoto.texture as Texture2D;
                    book_photo = texture.EncodeToPNG();
                }

                byte[] isbn_photo = null;
                if (this.isbnPhoto.texture != null)
                {
                    Texture2D texture = this.isbnPhoto.texture as Texture2D;
                    isbn_photo = texture.EncodeToPNG();
                }

                command.Parameters.Add("B_TITLE", OracleDbType.NVarchar2).Value = title.text;
                command.Parameters.Add("B_AUTHOR", OracleDbType.NVarchar2).Value = author.text;
                command.Parameters.Add("B_PUBLISHER", OracleDbType.NVarchar2).Value = publisher.text;
                command.Parameters.Add("B_PRICE", OracleDbType.Decimal).Value = price.text;
                command.Parameters.Add("B_URL", OracleDbType.Varchar2).Value = url.text;
                command.Parameters.Add("B_BOOKPHOTO", OracleDbType.Blob).Value = (book_photo == null) ? DBNull.Value : book_photo;
                command.Parameters.Add("B_ISBN", OracleDbType.Decimal).Value = isbn.text;
                command.Parameters.Add("B_ISBNPHOTO", OracleDbType.Blob).Value = (isbn_photo == null) ? DBNull.Value : isbn_photo;
                command.Parameters.Add("B_DESCRIPTION", OracleDbType.NVarchar2).Value = description.text;

                command.ExecuteNonQuery();

                successUI.SetActive(true);
            }
            catch (Exception ex) // 데이터 삽입 실패 시 예외 처리
            {
                failText.text = ex.Message;
                failUI.SetActive(true);
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

    public void ClosePopUp()
    {
        successUI.SetActive(false);
        failUI.SetActive(false);
    }
}
